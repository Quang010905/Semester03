using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using Semester03.Services.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize]
    public class TicketController : ClientBaseController
    {
        private readonly TicketRepository _ticketRepo;
        private readonly EventBookingRepository _eventBookingRepo;
        private readonly TicketEmailService _ticketEmailService;
        private readonly AbcdmallContext _context;

        public TicketController(
            TenantTypeRepository tenantTypeRepo,
            TicketRepository ticketRepo,
            EventBookingRepository eventBookingRepo,
            TicketEmailService ticketEmailService,
            AbcdmallContext context
        ) : base(tenantTypeRepo)
        {
            _ticketRepo = ticketRepo;
            _eventBookingRepo = eventBookingRepo;
            _ticketEmailService = ticketEmailService;
            _context = context;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        // ===============================================
        //        🎟  MY TICKETS — MOVIE + EVENTS
        // ===============================================
        public async Task<IActionResult> MyTickets()
        {
            int userId = GetUserId();
            DateTime now = DateTime.Now;

            // =======================
            // 1) MOVIE TICKETS (GROUP)
            // =======================
            var movieDb = await _ticketRepo.GetTicketsByUserAsync(userId);

            var movieTickets = movieDb
                .GroupBy(t =>
                {
                    var showtime = t.TicketShowtimeSeat.ShowtimeSeatShowtime;
                    var movie = showtime.ShowtimeMovie;

                    string status =
                        string.Equals(t.TicketStatus, "cancelled", StringComparison.OrdinalIgnoreCase) ? "Cancelled"
                        : showtime.ShowtimeStart <= now ? "Watched"
                        : "Upcoming";

                    return new
                    {
                        MovieTitle = movie.MovieTitle,
                        Showtime = showtime.ShowtimeStart,
                        ScreenName = showtime.ShowtimeScreen.ScreenName,
                        PosterUrl = movie.MovieImg,
                        Status = status
                    };
                })
                .Select(g =>
                {
                    var sample = g.First();
                    return new MyTicketVm
                    {
                        TicketId = sample.TicketId,
                        MovieTitle = g.Key.MovieTitle,
                        Showtime = g.Key.Showtime,
                        ScreenName = g.Key.ScreenName,
                        Price = sample.TicketPrice,
                        Status = g.Key.Status,
                        CreatedAt = sample.TicketCreatedAt ?? g.Key.Showtime,
                        PosterUrl = g.Key.PosterUrl,
                        Quantity = g.Count()
                    };
                })
                .OrderByDescending(x => x.Showtime)
                .ToList();

            // =======================
            // 2) EVENT TICKETS
            // =======================
            var eventDb = await _eventBookingRepo.GetBookingsForUserAsync(userId);

            // Lấy danh sách BookingId để query lịch sử huỷ (PartialCancel)
            var bookingIds = eventDb
                .Select(e => e.EventBookingId)
                .ToList();

            // Dictionary: BookingId -> tổng số vé đã huỷ (sum quantity PartialCancel)
            var cancelledQtyDict = _context.TblEventBookingHistories
                .Where(h =>
                    bookingIds.Contains((int)h.EventBookingHistoryBookingId) &&
                    h.EventBookingHistoryAction == "PartialCancel")
                .GroupBy(h => h.EventBookingHistoryBookingId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.EventBookingHistoryQuantity ?? 0)
                );

            var eventTickets = new List<MyEventTicketVm>();

            foreach (var e in eventDb)
            {
                var ev = e.EventBookingEvent;
                var unitPrice = e.EventBookingUnitPrice ?? 0m;

                int activeQty = e.EventBookingQuantity ?? 0;         // vé còn dùng
                int cancelledQty = 0;

                cancelledQtyDict.TryGetValue(e.EventBookingId, out cancelledQty);

                bool isFullyCancelled =
                    e.EventBookingStatus == 0            // status Cancelled
                    || activeQty <= 0;                  // hoặc quantity 0

                // ----------- 1. VÉ CÒN DÙNG (UPCOMING / OCCURRED) -----------
                if (!isFullyCancelled && activeQty > 0)
                {
                    string activeStatus =
                        ev.EventEnd <= now ? "Occurred" : "Upcoming";

                    eventTickets.Add(new MyEventTicketVm
                    {
                        BookingId = e.EventBookingId,
                        EventName = ev.EventName,
                        EventImage = ev.EventImg,
                        EventStart = ev.EventStart,
                        EventEnd = ev.EventEnd,
                        Quantity = activeQty,
                        TotalCost = activeQty * unitPrice,
                        Status = activeStatus
                    });
                }

                // ----------- 2. DÒNG TỔNG HỢP VÉ ĐÃ HUỶ (CHO TAB CANCELLED) -----------
                // Nếu booking đã huỷ hẳn hoặc có bất kỳ history PartialCancel nào
                if (isFullyCancelled || cancelledQty > 0)
                {
                    // Nếu full cancel mà chưa có history PartialCancel (trường hợp cũ),
                    // fallback: lấy số vé ban đầu = activeQty + cancelledQty (nếu có)
                    if (cancelledQty == 0)
                    {
                        cancelledQty = activeQty; // best-effort
                    }

                    var cancelledTotal = cancelledQty * unitPrice;

                    eventTickets.Add(new MyEventTicketVm
                    {
                        BookingId = e.EventBookingId,
                        EventName = ev.EventName,
                        EventImage = ev.EventImg,
                        EventStart = ev.EventStart,
                        EventEnd = ev.EventEnd,
                        Quantity = cancelledQty,
                        TotalCost = cancelledTotal,
                        Status = "Cancelled"  // để view gom vào tab Cancelled
                    });
                }
            }

            eventTickets = eventTickets
                .OrderByDescending(x => x.EventStart)
                .ToList();

            return View(new MyTicketsPageVm
            {
                MovieTickets = movieTickets,
                EventTickets = eventTickets
            });
        }

        // ===============================================
        //          ❌ CANCEL MOVIE TICKET (24H RULE)
        // ===============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelTicket(int id)
        {
            int userId = GetUserId();
            var ticket = await _ticketRepo.GetByIdAsync(id);

            if (ticket == null || ticket.TicketBuyerUserId != userId)
            {
                TempData["Error"] = "Ticket not found or ticket does not belong to you.";
                return RedirectToAction("MyTickets");
            }

            DateTime showtime = ticket.TicketShowtimeSeat.ShowtimeSeatShowtime.ShowtimeStart;

            if (showtime - DateTime.Now < TimeSpan.FromHours(24))
            {
                TempData["Error"] = "You can only cancel a movie ticket at least 24 hours in advance.";
                return RedirectToAction("MyTickets");
            }

            bool ok = await _ticketRepo.CancelTicketAsync(id);

            if (ok)
            {
                var buyer = ticket.TicketBuyerUser;
                var movie = ticket.TicketShowtimeSeat.ShowtimeSeatShowtime.ShowtimeMovie;

                var cancelledSeats = new List<string>
                {
                    ticket.TicketShowtimeSeat.ShowtimeSeatSeat.SeatLabel
                };

                decimal refundAmount = ticket.TicketPrice;

                await _ticketEmailService.SendMovieCancelEmailAsync(
                    userId: buyer.UsersId,
                    movieName: movie.MovieTitle,
                    showtime: showtime,
                    cancelledSeats: cancelledSeats,
                    refundAmount: refundAmount
                );
            }

            TempData[ok ? "Success" : "Error"] =
                ok ? "Cancellation successful!" : "Cancellation failed!";

            return RedirectToAction("Index", "TicketDetail", new { area = "Client", id = ticket.TicketId });
        }
    }
}
