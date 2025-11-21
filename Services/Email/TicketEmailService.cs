using Microsoft.Extensions.Logging;
using Semester03.Models.Entities;
using Semester03.Infrastructure;
using Semester03.Areas.Client.Models.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Semester03.Models.Repositories;
using System;

namespace Semester03.Services.Email
{
    public class TicketEmailService
    {
        private readonly AbcdmallContext _db;
        private readonly RazorViewToStringRenderer _renderer;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<TicketEmailService> _logger;

        public TicketEmailService(
            AbcdmallContext db,
            RazorViewToStringRenderer renderer,
            IEmailSender emailSender,
            ILogger<TicketEmailService> logger)
        {
            _db = db;
            _renderer = renderer;
            _emailSender = emailSender;
            _logger = logger;
        }

        /// <summary>
        /// Overload cũ – nếu ở nơi khác gọi kiểu (userId, showtimeSeatIds) thì vẫn chạy được.
        /// Sẽ tự tính tiền gốc, không có khuyến mãi.
        /// </summary>
        public Task SendTicketsEmailAsync(int userId, List<int> showtimeSeatIds)
        {
            // Không có thông tin giảm giá => truyền null, null, null
            return SendTicketsEmailAsync(userId, showtimeSeatIds, null, null, null);
        }

        /// <summary>
        /// Overload mới – cho phép truyền vào tổng gốc, giảm giá, tổng cuối.
        /// Nếu tham số null sẽ tự tính lại từ danh sách vé.
        /// </summary>
        public async Task SendTicketsEmailAsync(
            int userId,
            List<int> showtimeSeatIds,
            decimal? originalAmount,
            decimal? discountAmount,
            decimal? finalAmount)
        {
            var user = await _db.TblUsers.FindAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.UsersEmail))
            {
                _logger.LogWarning("SendTicketsEmailAsync: user {UserId} not found or has no email.", userId);
                return;
            }

            if (showtimeSeatIds == null || !showtimeSeatIds.Any())
            {
                _logger.LogWarning("SendTicketsEmailAsync: no showtimeSeatIds provided for user {UserId}.", userId);
                return;
            }

            // Lấy chi tiết vé từ repository (kiểu List<TicketEmailItem>)
            var repo = new TicketRepository(_db);
            var ticketDetails = await repo.GetTicketDetailsByShowtimeSeatIdsAsync(showtimeSeatIds);

            if (ticketDetails == null || !ticketDetails.Any())
            {
                _logger.LogWarning("SendTicketsEmailAsync: no ticket details resolved for user {UserId}.", userId);
                return;
            }

            // MAP từ TicketEmailItem -> TicketEmailItemVm cho đúng kiểu ViewModel
            var ticketVmList = ticketDetails
                .Select(t => new TicketEmailItemVm
                {
                    MovieTitle = t.MovieTitle,
                    CinemaName = t.CinemaName,
                    ScreenName = t.ScreenName,
                    ShowtimeStart = t.ShowtimeStart,
                    SeatLabel = t.SeatLabel,
                    Price = t.Price
                })
                .ToList();

            // Tổng từ dữ liệu vé (fallback nếu không truyền tham số)
            var baseTotal = ticketVmList.Sum(t => t.Price);

            // Nếu controller truyền vào thì ưu tiên dùng, nếu không thì dùng baseTotal
            var effectiveOriginal = originalAmount ?? baseTotal;
            var effectiveFinal = finalAmount ?? baseTotal;

            var effectiveDiscount = discountAmount ?? (effectiveOriginal - effectiveFinal);
            if (effectiveDiscount < 0) effectiveDiscount = 0m;

            // Điểm thưởng tính dựa trên số tiền thực trả
            var pointsAwarded = (int)Math.Floor(effectiveFinal / 100m);

            var model = new TicketEmailViewModel
            {
                UserFullName = string.IsNullOrWhiteSpace(user.UsersFullName)
                    ? user.UsersUsername
                    : user.UsersFullName,
                PurchaseDate = DateTime.UtcNow,
                Tickets = ticketVmList,              // <-- giờ đã đúng kiểu List<TicketEmailItemVm>
                OriginalAmount = effectiveOriginal,
                DiscountAmount = effectiveDiscount,
                TotalAmount = effectiveFinal,
                PointsAwarded = pointsAwarded
            };

            var html = await _renderer.RenderViewToStringAsync(
                "~/Areas/Client/Views/Emails/TicketEmail.cshtml", model);

            try
            {
                await _emailSender.SendEmailAsync(
                    user.UsersEmail,
                    "Vé ABCD Mall - Đơn hàng của bạn",
                    html);

                _logger.LogInformation("Ticket email sent to {Email}", user.UsersEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending tickets email to {Email}", user.UsersEmail);
                throw;
            }
        }
    }
}
