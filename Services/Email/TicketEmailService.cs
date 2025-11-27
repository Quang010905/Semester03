using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Infrastructure;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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


        // ====================================================================
        // 1) EMAIL VÉ XEM PHIM
        // ====================================================================
        public Task SendTicketsEmailAsync(int userId, List<int> showtimeSeatIds)
        {
            return SendTicketsEmailAsync(userId, showtimeSeatIds, null, null, null);
        }

        public async Task SendTicketsEmailAsync(
            int userId,
            List<int> showtimeSeatIds,
            decimal? originalAmount,
            decimal? discountAmount,
            decimal? finalAmount)
        {
            var user = await _db.TblUsers.FindAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.UsersEmail))
                return;

            var repo = new TicketRepository(_db);
            var ticketDetails = await repo.GetTicketDetailsByShowtimeSeatIdsAsync(showtimeSeatIds);

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

            var baseTotal = ticketVmList.Sum(t => t.Price);
            var effectiveOriginal = originalAmount ?? baseTotal;
            var effectiveFinal = finalAmount ?? baseTotal;
            var effectiveDiscount = discountAmount ?? (effectiveOriginal - effectiveFinal);

            var model = new TicketEmailViewModel
            {
                UserFullName = user.UsersFullName ?? user.UsersUsername,
                PurchaseDate = DateTime.UtcNow,
                Tickets = ticketVmList,
                OriginalAmount = effectiveOriginal,
                DiscountAmount = effectiveDiscount,
                TotalAmount = effectiveFinal,
                PointsAwarded = (int)Math.Floor(effectiveFinal / 100m)
            };

            var html = await _renderer.RenderViewToStringAsync(
                "~/Areas/Client/Views/Emails/TicketEmail.cshtml",
                model);

            await _emailSender.SendEmailAsync(
                user.UsersEmail,
                "Vé ABCD Mall - Đơn hàng của bạn",
                html);
        }


        // ====================================================================
        // 2) EMAIL HỦY VÉ XEM PHIM  (NEW)
        // ====================================================================
        public async Task SendMovieCancelEmailAsync(
            int userId,
            string movieName,
            DateTime showtime,
            List<string> cancelledSeats,
            decimal refundAmount)
        {
            var user = await _db.TblUsers.FindAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.UsersEmail)) return;

            var model = new MovieCancelEmailVm
            {
                UserName = user.UsersFullName ?? user.UsersUsername,
                MovieName = movieName,
                Showtime = showtime,
                CancelledSeats = cancelledSeats,
                RefundAmount = refundAmount
            };

            var html = await _renderer.RenderViewToStringAsync(
                "~/Areas/Client/Views/Emails/MovieCancelEmail.cshtml",
                model);

            await _emailSender.SendEmailAsync(
                user.UsersEmail,
                "Hủy vé xem phim – Xác nhận",
                html);
        }


        // ====================================================================
        // 3) EMAIL HỦY VÉ SỰ KIỆN (NEW — TEMPLATE)
        // ====================================================================
        public async Task SendEventCancelEmailAsync(
    int userId,
    string eventName,
    int bookingId,
    int cancelledQty,
    int remainingQty,
    decimal refundAmount)
        {
            // dùng _db chứ không phải _context
            var user = await _db.TblUsers.FindAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.UsersEmail))
            {
                _logger.LogWarning("SendEventCancelEmailAsync: user {UserId} not found or has no email.", userId);
                return;
            }

            // chuẩn bị ViewModel cho email
            var model = new EventCancelEmailVm
            {
                UserName = string.IsNullOrWhiteSpace(user.UsersFullName)
                                    ? user.UsersUsername
                                    : user.UsersFullName,
                EventName = eventName,
                BookingId = bookingId,
                CancelledQty = cancelledQty,
                RemainingQty = remainingQty,
                RefundAmount = refundAmount          // 👈 rất quan trọng
            };

            // dùng _renderer chứ không phải _viewRenderer
            string html = await _renderer.RenderViewToStringAsync(
                "~/Areas/Client/Views/Emails/EventCancelEmail.cshtml",
                model
            );

            string subject = $"Hủy vé sự kiện – Mã #{bookingId}";

            try
            {
                await _emailSender.SendEmailAsync(user.UsersEmail, subject, html);
                _logger.LogInformation("Event cancel email sent to {Email}", user.UsersEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending event cancel email to {Email}", user.UsersEmail);
                throw;
            }
        }



    }
}
