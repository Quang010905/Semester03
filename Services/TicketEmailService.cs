using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using Semester03.Services.Email;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Semester03.Services
{
    public class TicketEmailService
    {
        private readonly ILogger<TicketEmailService> _logger;
        private readonly AbcdmallContext _context;
        private readonly TicketRepository _ticketRepo;
        private readonly IEmailSender _emailSender;
        private readonly RazorViewToStringRenderer _razorRenderer;

        public TicketEmailService(
            ILogger<TicketEmailService> logger,
            AbcdmallContext context,
            TicketRepository ticketRepo,
            IEmailSender emailSender,
            RazorViewToStringRenderer razorRenderer)
        {
            _logger = logger;
            _context = context;
            _ticketRepo = ticketRepo;
            _emailSender = emailSender;
            _razorRenderer = razorRenderer;
        }

        public async Task SendTicketsEmailAsync(int userId, List<int> showtimeSeatIds)
        {
            try
            {
                _logger.LogInformation("Preparing ticket email for user {UserId} seats {Seats}", userId, string.Join(",", showtimeSeatIds));

                var user = await _context.TblUsers.FirstOrDefaultAsync(u => u.UsersId == userId);
                if (user == null || string.IsNullOrEmpty(user.UsersEmail))
                {
                    _logger.LogWarning("User email not found for user {UserId}", userId);
                    return;
                }

                var tickets = await _ticketRepo.GetTicketDetailsByShowtimeSeatIdsAsync(showtimeSeatIds);
                if (tickets == null || tickets.Count == 0)
                {
                    _logger.LogWarning("No tickets found to email for user {UserId}", userId);
                    return;
                }

                var model = new TicketEmailViewModel
                {
                    UserFullName = user.UsersFullName ?? "Khách hàng",
                    PurchaseDate = DateTime.UtcNow,
                    Tickets = tickets,
                    TotalAmount = tickets.Sum(x => x.Price),
                    PointsAwarded = (int)(tickets.Sum(x => x.Price) / 100)
                };

                string html = await _razorRenderer.RenderViewToStringAsync("/Areas/Client/Views/Emails/TicketEmail.cshtml", model);

                await _emailSender.SendEmailAsync(user.UsersEmail, "🎟️ Vé xem phim tại ABCD Mall", html);
                _logger.LogInformation("Ticket email sent successfully to {Email}", user.UsersEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in SendTicketsEmailAsync for user {UserId}", userId);
            }
        }
    }
}
