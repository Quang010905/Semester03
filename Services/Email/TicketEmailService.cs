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
        // 1) MOVIE TICKET EMAIL
        // ====================================================================

        /// <summary>
        /// Old overload: only showtimeSeatIds, no discount info.
        /// </summary>
        public Task SendTicketsEmailAsync(int userId, List<int> showtimeSeatIds)
        {
            return SendTicketsEmailAsync(userId, showtimeSeatIds, null, null, null);
        }

        /// <summary>
        /// Send movie ticket email + total / discount / reward points.
        /// </summary>
        public async Task SendTicketsEmailAsync(
            int userId,
            List<int> showtimeSeatIds,
            decimal? originalAmount,
            decimal? discountAmount,
            decimal? finalAmount)
        {
            if (showtimeSeatIds == null || !showtimeSeatIds.Any())
            {
                _logger.LogWarning("SendTicketsEmailAsync: empty showtimeSeatIds for user {UserId}", userId);
                return;
            }

            var user = await _db.TblUsers.FindAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.UsersEmail))
            {
                _logger.LogWarning("SendTicketsEmailAsync: user {UserId} not found or has no email.", userId);
                return;
            }

            var repo = new TicketRepository(_db);
            var ticketDetails = await repo.GetTicketDetailsByShowtimeSeatIdsAsync(showtimeSeatIds);

            if (ticketDetails == null || !ticketDetails.Any())
            {
                _logger.LogWarning("SendTicketsEmailAsync: no ticket details found for user {UserId}", userId);
                return;
            }

            var ticketVmList = ticketDetails
                .Select(t =>
                {
                    var qrPayload =
                        $"TICKET|MOVIE={t.MovieTitle}|CINEMA={t.CinemaName}|SCREEN={t.ScreenName}|TIME={t.ShowtimeStart:yyyy-MM-dd HH:mm}|SEAT={t.SeatLabel}";

                    return new TicketEmailItemVm
                    {
                        MovieTitle = t.MovieTitle,
                        CinemaName = t.CinemaName,
                        ScreenName = t.ScreenName,
                        ShowtimeStart = t.ShowtimeStart,
                        SeatLabel = t.SeatLabel,
                        Price = t.Price,
                        QrCode = qrPayload
                    };
                })
                .ToList();

            var baseTotal = ticketVmList.Sum(t => t.Price);

            var effectiveOriginal = originalAmount ?? baseTotal;
            var effectiveFinal = finalAmount ?? baseTotal;
            var effectiveDiscount = discountAmount ?? (effectiveOriginal - effectiveFinal);
            if (effectiveDiscount < 0) effectiveDiscount = 0;

            var pointsAwarded = (int)Math.Floor(effectiveFinal / 100m);
            var purchaseDateUtc = DateTime.UtcNow;

            var model = new TicketEmailViewModel
            {
                UserFullName = user.UsersFullName ?? user.UsersUsername,
                PurchaseDate = purchaseDateUtc,
                Tickets = ticketVmList,
                OriginalAmount = effectiveOriginal,
                DiscountAmount = effectiveDiscount,
                TotalAmount = effectiveFinal,
                PointsAwarded = pointsAwarded
            };

            string html;
            try
            {
                html = await _renderer.RenderViewToStringAsync(
                    "~/Areas/Client/Views/Emails/TicketEmail.cshtml",
                    model);
            }
            catch (Exception exRender)
            {
                _logger.LogError(exRender, "SendTicketsEmailAsync: error rendering TicketEmail view for user {UserId}", userId);
                throw;
            }

            try
            {
                await _emailSender.SendEmailAsync(
                    user.UsersEmail,
                    "ABCD Mall Tickets – Your order",
                    html);

                _logger.LogInformation("SendTicketsEmailAsync: ticket email sent to {Email}", user.UsersEmail);
            }
            catch (Exception exSend)
            {
                _logger.LogError(exSend, "SendTicketsEmailAsync: error sending ticket email to {Email}", user.UsersEmail);
                throw;
            }
        }

        // ====================================================================
        // 2) MOVIE TICKET CANCELLATION EMAIL
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
                "Movie ticket cancellation – Confirmation",
                html);
        }

        // ====================================================================
        // 3) EVENT TICKET CANCELLATION EMAIL
        // ====================================================================
        public async Task SendEventCancelEmailAsync(
            int userId,
            string eventName,
            int bookingId,
            int cancelledQty,
            int remainingQty,
            decimal refundAmount)
        {
            var user = await _db.TblUsers.FindAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.UsersEmail))
            {
                _logger.LogWarning("SendEventCancelEmailAsync: user {UserId} not found or has no email.", userId);
                return;
            }

            var model = new EventCancelEmailVm
            {
                UserName = string.IsNullOrWhiteSpace(user.UsersFullName)
                    ? user.UsersUsername
                    : user.UsersFullName,
                EventName = eventName,
                BookingId = bookingId,
                CancelledQty = cancelledQty,
                RemainingQty = remainingQty,
                RefundAmount = refundAmount
            };

            string html = await _renderer.RenderViewToStringAsync(
                "~/Areas/Client/Views/Emails/EventCancelEmail.cshtml",
                model);

            string subject = $"Event cancellation – Booking #{bookingId}";

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

        // ====================================================================
        // 4) EVENT BOOKING SUCCESS EMAIL
        // ====================================================================
        public async Task SendEventBookingSuccessEmailAsync(int bookingId)
        {
            var booking = await _db.TblEventBookings
                .Include(b => b.EventBookingEvent)
                .FirstOrDefaultAsync(b => b.EventBookingId == bookingId);

            if (booking == null)
            {
                _logger.LogWarning("SendEventBookingSuccessEmailAsync: booking {BookingId} not found.", bookingId);
                return;
            }

            var user = await _db.TblUsers.FindAsync(booking.EventBookingUserId);
            if (user == null || string.IsNullOrWhiteSpace(user.UsersEmail))
            {
                _logger.LogWarning("SendEventBookingSuccessEmailAsync: user {UserId} not found or no email.",
                    booking.EventBookingUserId);
                return;
            }

            var ev = booking.EventBookingEvent;
            if (ev == null)
            {
                _logger.LogWarning("SendEventBookingSuccessEmailAsync: event not loaded for booking {BookingId}.", bookingId);
                return;
            }

            string organizerName = "ABCD Mall";
            string location = string.Empty;

            try
            {
                var eventRepo = new EventRepository(_db);
                var evtDetail = await eventRepo.GetEventByIdAsync(ev.EventId, booking.EventBookingUserId);

                if (evtDetail != null)
                {
                    if (!string.IsNullOrWhiteSpace(evtDetail.OrganizerShopName))
                    {
                        organizerName = evtDetail.OrganizerShopName;
                    }

                    if (!string.IsNullOrWhiteSpace(evtDetail.PositionLocation))
                    {
                        location = evtDetail.PositionLocation;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SendEventBookingSuccessEmailAsync: error when reading organizer/location from EventRepository");
            }

            decimal unitPrice = booking.EventBookingUnitPrice ?? 0m;
            int qty = booking.EventBookingQuantity ?? 1;
            decimal totalAmount = booking.EventBookingTotalCost ?? (unitPrice * qty);

            var model = new EventBookingEmailVm
            {
                UserFullName = string.IsNullOrWhiteSpace(user.UsersFullName)
                    ? user.UsersUsername
                    : user.UsersFullName,

                BookingId = booking.EventBookingId,

                EventName = ev.EventName,
                EventStart = ev.EventStart,
                EventEnd = ev.EventEnd,

                Location = location,
                OrganizerName = organizerName,

                Quantity = qty,
                UnitPrice = unitPrice,
                TotalAmount = totalAmount,

                PurchaseDate = DateTime.UtcNow
            };

            string html = await _renderer.RenderViewToStringAsync(
                "~/Areas/Client/Views/Emails/EventBookingEmail.cshtml",
                model);

            string subject = $"ABCD Mall – Event booking confirmation #{booking.EventBookingId}";

            try
            {
                await _emailSender.SendEmailAsync(user.UsersEmail, subject, html);
                _logger.LogInformation("Event booking email sent to {Email}", user.UsersEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending event booking email to {Email}", user.UsersEmail);
                throw;
            }
        }
    }
}
