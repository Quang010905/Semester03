using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Repositories;
using Semester03.Areas.Client.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize]
    public class TicketController : ClientBaseController
    {
        private readonly TicketRepository _ticketRepo;
        private readonly EventBookingRepository _eventBookingRepo;

        public TicketController(
            TenantTypeRepository tenantTypeRepo,      // 👈 thêm để truyền vào base
            TicketRepository ticketRepo,
            EventBookingRepository eventBookingRepo
        ) : base(tenantTypeRepo)                     // 👈 gọi constructor của controller cha
        {
            _ticketRepo = ticketRepo;
            _eventBookingRepo = eventBookingRepo;
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
            // 1) VÉ XEM PHIM
            // =======================
            var movieDb = await _ticketRepo.GetTicketsByUserAsync(userId);

            var movieTickets = movieDb.Select(t =>
            {
                DateTime show = t.TicketShowtimeSeat.ShowtimeSeatShowtime.ShowtimeStart;

                string status =
                    t.TicketStatus == "cancelled" ? "Đã hủy"
                    : show <= now ? "Đã xem"
                    : "Sắp chiếu";

                return new MyTicketVm
                {
                    TicketId = t.TicketId,
                    MovieTitle = t.TicketShowtimeSeat.ShowtimeSeatShowtime.ShowtimeMovie.MovieTitle,
                    Showtime = show,
                    SeatLabel = t.TicketShowtimeSeat.ShowtimeSeatSeat.SeatLabel,
                    ScreenName = t.TicketShowtimeSeat.ShowtimeSeatShowtime.ShowtimeScreen.ScreenName,
                    Price = t.TicketPrice,
                    Status = status,
                    CreatedAt = (DateTime)t.TicketCreatedAt,

                    // 👇 lấy đúng field ảnh từ TblMovie
                    PosterUrl = t.TicketShowtimeSeat
                     .ShowtimeSeatShowtime
                     .ShowtimeMovie
                     .MovieImg
                };

            }).ToList();


            // =======================
            // 2) VÉ SỰ KIỆN
            // =======================
            var eventDb = await _eventBookingRepo.GetBookingsForUserAsync(userId);

            var eventTickets = eventDb.Select(e =>
            {
                var ev = e.EventBookingEvent;

                string status =
                    e.EventBookingStatus == 0 ? "Đã hủy"
                    : ev.EventEnd <= now ? "Đã diễn ra"
                    : "Sắp diễn ra";

                return new MyEventTicketVm
                {
                    BookingId = e.EventBookingId,
                    EventName = ev.EventName,
                    EventImage = ev.EventImg,

                    // Event_Start, Event_End là DATETIME2 → EF map thành DateTime
                    EventStart = ev.EventStart,
                    EventEnd = ev.EventEnd,

                    // FIX NULLABLE:
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
                TempData["Error"] = "Không tìm thấy vé hoặc vé không thuộc về bạn.";
                return RedirectToAction("MyTickets");
            }

            DateTime show = ticket.TicketShowtimeSeat.ShowtimeSeatShowtime.ShowtimeStart;

            if (show - DateTime.Now < TimeSpan.FromHours(24))
            {
                TempData["Error"] = "Bạn chỉ được hủy vé trước 24 giờ.";
                return RedirectToAction("MyTickets");
            }

            bool ok = await _ticketRepo.CancelTicketAsync(id);

            TempData[ok ? "Success" : "Error"] =
                ok ? "Hủy vé thành công!" : "Hủy vé thất bại!";

            return RedirectToAction("MyTickets");
        }
    }
}
