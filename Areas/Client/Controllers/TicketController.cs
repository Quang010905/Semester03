using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Repositories;
using Semester03.Services.Email;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize]
    public class TicketController : ClientBaseController
    {
        private readonly TicketRepository _ticketRepo;
        private readonly EventBookingRepository _eventBookingRepo;
        private readonly TicketEmailService _ticketEmailService;

        public TicketController(
            TenantTypeRepository tenantTypeRepo,
            TicketRepository ticketRepo,
            EventBookingRepository eventBookingRepo,
            TicketEmailService ticketEmailService
        ) : base(tenantTypeRepo)
        {
            _ticketRepo = ticketRepo;
            _eventBookingRepo = eventBookingRepo;
            _ticketEmailService = ticketEmailService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        // ===============================================
        //        🎟  VÉ CỦA TÔI — 2 TAB LỚN
        // ===============================================
        public async Task<IActionResult> MyTickets()
        {
            int userId = GetUserId();
            DateTime now = DateTime.Now;

            // =======================
            // 1) VÉ XEM PHIM (GROUP)
            // =======================
            var movieDb = await _ticketRepo.GetTicketsByUserAsync(userId);

            // Gộp theo: Phim + thời gian chiếu + phòng + poster + status
            var movieTickets = movieDb
                .GroupBy(t =>
                {
                    var showtime = t.TicketShowtimeSeat.ShowtimeSeatShowtime;
                    var movie = showtime.ShowtimeMovie;

                    string status =
                        t.TicketStatus == "cancelled" ? "Canceled"
                        : showtime.ShowtimeStart <= now ? "Seen"
                        : "Coming up";

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
                        // dùng 1 TicketId đại diện để đi tới trang chi tiết
                        TicketId = sample.TicketId,
                        MovieTitle = g.Key.MovieTitle,
                        Showtime = g.Key.Showtime,
                        ScreenName = g.Key.ScreenName,
                        // giá: lấy giá của 1 vé (giả sử giống nhau)
                        Price = sample.TicketPrice,
                        Status = g.Key.Status,
                        CreatedAt = (DateTime)sample.TicketCreatedAt,
                        PosterUrl = g.Key.PosterUrl,
                        Quantity = g.Count()
                        // SeatLabel ở màn danh sách không dùng nữa,
                        // nếu cần có thể nối string ghế tại đây.
                    };
                })
                .OrderByDescending(x => x.Showtime)
                .ToList();

            // =======================
            // 2) VÉ SỰ KIỆN (giữ nguyên)
            // =======================
            var eventDb = await _eventBookingRepo.GetBookingsForUserAsync(userId);

            var eventTickets = eventDb.Select(e =>
            {
                var ev = e.EventBookingEvent;

                string status =
                    e.EventBookingStatus == 0 ? "Cancelled"
                    : ev.EventEnd <= now ? "Occurred"
                    : "Coming up";

                return new MyEventTicketVm
                {
                    BookingId = e.EventBookingId,
                    EventName = ev.EventName,
                    EventImage = ev.EventImg,
                    EventStart = ev.EventStart,
                    EventEnd = ev.EventEnd,
                    Quantity = e.EventBookingQuantity ?? 1,
                    TotalCost = e.EventBookingTotalCost ?? 0m,
                    Status = status
                };
            }).ToList();

            return View(new MyTicketsPageVm
            {
                MovieTickets = movieTickets,
                EventTickets = eventTickets
            });
        }

        // ===============================================
        //          ❌ HỦY VÉ XEM PHIM (KHÓA 24H)
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
                TempData["Error"] = "You can only cancel the ticket at least 24 hours in advance.";
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
                ok ? "Cancel thành công!" : "Cancel thất bại!";

            return RedirectToAction("Index", "TicketDetail", new { area = "Client", id = ticket.TicketId });
        }
    }
}
