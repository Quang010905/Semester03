using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Semester03.Models.Repositories;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Services.Email;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize]
    public class TicketDetailController : ClientBaseController
    {
        private readonly TicketRepository _ticketRepo;
        private readonly TicketEmailService _ticketEmailService;

        public TicketDetailController(
            TenantTypeRepository tenantTypeRepo,
            TicketRepository ticketRepo,
            TicketEmailService ticketEmailService
        ) : base(tenantTypeRepo)
        {
            _ticketRepo = ticketRepo;
            _ticketEmailService = ticketEmailService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        // ================================
        // TRANG CHI TIẾT VÉ + DANH SÁCH GHẾ
        // ================================
        public async Task<IActionResult> Index(int id)
        {
            int userId = GetUserId();

            var ticket = await _ticketRepo.GetByIdAsync(id);

            if (ticket == null || ticket.TicketBuyerUserId != userId)
                return NotFound();

            var st = ticket.TicketShowtimeSeat.ShowtimeSeatShowtime;
            var mv = st.ShowtimeMovie;

            bool isUsed = st.ShowtimeStart <= DateTime.Now;

            string qrUrl = Url.Action("Index", "TicketDetail",
                new { area = "Client", id = ticket.TicketId },
                protocol: HttpContext.Request.Scheme);

            string qrImg = "https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=" +
                           System.Net.WebUtility.UrlEncode(qrUrl);

            // Lấy toàn bộ vé cùng suất chiếu của user
            var allTicketsOfUser = await _ticketRepo.GetTicketsByUserAsync(userId);

            var sameShowtimeTickets = allTicketsOfUser
                .Where(x => x.TicketShowtimeSeat.ShowtimeSeatShowtime.ShowtimeId == st.ShowtimeId)
                .ToList();

            var vm = new TicketDetailVm
            {
                ShowtimeId = st.ShowtimeId,
                TicketId = ticket.TicketId,
                MovieTitle = mv.MovieTitle,
                MovieImg = mv.MovieImg ?? "/images/movie-placeholder.jpg",
                Duration = mv.MovieDurationMin,
                Director = mv.MovieDirector,
                Genre = mv.MovieGenre,
                Description = mv.MovieDescription ?? "",
                Showtime = st.ShowtimeStart,
                EndTime = st.ShowtimeStart.AddMinutes(mv.MovieDurationMin),
                Screen = st.ShowtimeScreen.ScreenName,
                TheaterName = "ABCD Mall Cinema",
                TheaterAddress = "123 Main Street, City",
                QRCodeUrl = qrImg,
                IsUsed = isUsed,

                Seats = sameShowtimeTickets
                    .Where(x => x.TicketStatus != "cancelled")
                    .Select(x => new TicketSeatVm
                    {
                        TicketId = x.TicketId,
                        SeatLabel = x.TicketShowtimeSeat.ShowtimeSeatSeat.SeatLabel,
                        Price = x.TicketPrice,
                        Status = x.TicketStatus
                    }).ToList(),

                CancelledSeats = sameShowtimeTickets
                    .Where(x => x.TicketStatus == "cancelled")
                    .Select(x => new TicketSeatVm
                    {
                        TicketId = x.TicketId,
                        SeatLabel = x.TicketShowtimeSeat.ShowtimeSeatSeat.SeatLabel,
                        Price = x.TicketPrice,
                        Status = x.TicketStatus
                    }).ToList()
            };

            return View(vm);
        }

        // ================================
        // API HỦY NHIỀU GHẾ
        // ================================
        [HttpPost]
        public async Task<IActionResult> CancelSelectedSeats([FromForm] int[] seatIds)
        {
            int userId = GetUserId();

            if (seatIds == null || seatIds.Length == 0)
                return Json(new { success = false, message = "Bạn chưa chọn ghế cần hủy." });

            var distinctIds = seatIds.Distinct().ToList();
            var tickets = new List<Semester03.Models.Entities.TblTicket>();

            foreach (var id in distinctIds)
            {
                var t = await _ticketRepo.GetByIdAsync(id);
                if (t != null)
                    tickets.Add(t);
            }

            if (!tickets.Any())
                return Json(new { success = false, message = "Không tìm thấy vé hợp lệ." });

            // Kiểm tra tất cả vé đều thuộc user hiện tại
            if (tickets.Any(t => t.TicketBuyerUserId != userId))
            {
                return Json(new
                {
                    success = false,
                    message = "Phát hiện vé không thuộc tài khoản hiện tại."
                });
            }

            // Giả sử tất cả vé cùng một suất chiếu
            var sample = tickets.First();
            var showtimeStart = sample.TicketShowtimeSeat.ShowtimeSeatShowtime.ShowtimeStart;

            // Khóa hủy trước 24h
            if (showtimeStart - DateTime.Now < TimeSpan.FromHours(24))
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn chỉ được hủy vé trước giờ chiếu 24 giờ."
                });
            }

            int cancelled = 0;
            decimal totalRefund = 0;
            var cancelledSeatLabels = new List<string>();

            foreach (var t in tickets)
            {
                // Nếu đã hủy rồi thì bỏ qua
                if (string.Equals(t.TicketStatus, "cancelled", StringComparison.OrdinalIgnoreCase))
                    continue;

                bool ok = await _ticketRepo.CancelTicketAsync(t.TicketId);
                if (!ok) continue;

                cancelled++;
                totalRefund += t.TicketPrice;
                cancelledSeatLabels.Add(t.TicketShowtimeSeat.ShowtimeSeatSeat.SeatLabel);
            }

            if (cancelled == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Không có ghế nào được hủy (có thể do đã bị hủy trước đó)."
                });
            }

            // =========================
            // GỬI EMAIL XÁC NHẬN HỦY
            // =========================
            try
            {
                var movie = sample.TicketShowtimeSeat.ShowtimeSeatShowtime.ShowtimeMovie;

                await _ticketEmailService.SendMovieCancelEmailAsync(
                    userId: userId,
                    movieName: movie.MovieTitle,
                    showtime: showtimeStart,
                    cancelledSeats: cancelledSeatLabels,
                    refundAmount: totalRefund
                );
            }
            catch
            {
                // Nếu gửi mail lỗi thì vẫn coi là hủy vé thành công
            }

            string msg = $"Đã hủy {cancelled} ghế, hoàn lại {totalRefund:N0}đ.";

            return Json(new
            {
                success = true,
                message = msg
            });
        }
    }
}
