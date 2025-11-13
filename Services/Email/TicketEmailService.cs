using Microsoft.Extensions.Logging;
using Semester03.Models.Entities;
using Semester03.Infrastructure;
using Semester03.Areas.Client.Models.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Semester03.Models.Repositories;

namespace Semester03.Services.Email
{
    public class TicketEmailService
    {
        private readonly AbcdmallContext _db;
        private readonly RazorViewToStringRenderer _renderer;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<TicketEmailService> _logger;

        public TicketEmailService(AbcdmallContext db, RazorViewToStringRenderer renderer, IEmailSender emailSender, ILogger<TicketEmailService> logger)
        {
            _db = db;
            _renderer = renderer;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task SendTicketsEmailAsync(int userId, List<int> showtimeSeatIds)
        {
            var user = await _db.TblUsers.FindAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.UsersEmail))
            {
                _logger.LogWarning("SendTicketsEmailAsync: user {UserId} not found or has no email.", userId);
                return;
            }

            // Build ticket details using the repository method
            var repo = new TicketRepository(_db);
            var ticketDetails = await repo.GetTicketDetailsByShowtimeSeatIdsAsync(showtimeSeatIds);

            var model = new TicketEmailViewModel
            {
                UserFullName = user.UsersFullName ?? user.UsersUsername,
                PurchaseDate = System.DateTime.UtcNow,
                Tickets = ticketDetails,
                TotalAmount = ticketDetails.Sum(t => t.Price),
                PointsAwarded = (int)System.Math.Floor(ticketDetails.Sum(t => t.Price) / 100m)
            };

            var html = await _renderer.RenderViewToStringAsync("~/Areas/Client/Views/Emails/TicketEmail.cshtml", model);

            try
            {
                await _emailSender.SendEmailAsync(user.UsersEmail, "Vé ABCD Mall - Đơn hàng của bạn", html);
                _logger.LogInformation("Ticket email sent to {Email}", user.UsersEmail);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending tickets email to {Email}", user.UsersEmail);
                throw;
            }
        }
    }
}
