using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Semester03.Models.Repositories
{
    public class EventBookingRepository
    {
        private readonly AbcdmallContext _db;

        public EventBookingRepository(AbcdmallContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        private IQueryable<TblEventBooking> GetFullBookingQuery()
        {
            return _db.TblEventBookings
                .Include(b => b.EventBookingEvent)
                .Include(b => b.EventBookingUser)
                .Include(b => b.EventBookingTenant);
        }

        public async Task<IEnumerable<TblEventBooking>> GetAllAsync()
        {
            return await GetFullBookingQuery()
                .OrderByDescending(b => b.EventBookingCreatedDate)
                .ToListAsync();
        }

        public async Task<TblEventBooking> GetByIdAsync(int id)
        {
            return await GetFullBookingQuery()
                .FirstOrDefaultAsync(b => b.EventBookingId == id);
        }

        public async Task<TblEventBooking> GetByIdNoTrackingAsync(int id)
        {
            return await _db.TblEventBookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.EventBookingId == id);
        }

        public async Task<IEnumerable<TblEventBooking>> GetBookingsForEventAsync(int eventId)
        {
            return await GetFullBookingQuery()
                .Where(b => b.EventBookingEventId == eventId)
                .OrderByDescending(b => b.EventBookingCreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<TblEventBooking>> GetBookingsForUserAsync(int userId)
        {
            return await GetFullBookingQuery()
                .Where(b => b.EventBookingUserId == userId)
                .OrderByDescending(b => b.EventBookingCreatedDate)
                .ToListAsync();
        }

        public async Task<TblEventBooking> CreateBookingAsync(TblEventBooking booking)
        {
            if (booking == null) throw new ArgumentNullException(nameof(booking));

            _db.TblEventBookings.Add(booking);
            await _db.SaveChangesAsync();
            return booking;
        }

        public async Task<TblEventBooking> CreateBookingAsync(int tenantId, int? userId, int eventId, decimal totalCost, int quantity = 1, string notes = "")
        {
            var combinedNotes = notes ?? "";
            if (!combinedNotes.Contains("Qty:", StringComparison.OrdinalIgnoreCase))
            {
                combinedNotes = $"Qty:{quantity}" + (string.IsNullOrWhiteSpace(combinedNotes) ? "" : ";" + combinedNotes);
            }

            var entity = new TblEventBooking
            {
                EventBookingTenantId = tenantId,
                EventBookingUserId = userId ?? 0,
                EventBookingEventId = eventId,
                EventBookingTotalCost = totalCost,
                EventBookingPaymentStatus = totalCost > 0m ? 0 : 2,
                EventBookingNotes = combinedNotes,
                EventBookingCreatedDate = DateTime.UtcNow
            };

            _db.TblEventBookings.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdatePaymentStatusAsync(int bookingId, int newStatus)
        {
            var booking = await _db.TblEventBookings.FindAsync(bookingId);
            if (booking == null) return false;

            booking.EventBookingPaymentStatus = newStatus;

            try
            {
                var prop = booking.GetType().GetProperty("EventBookingUpdatedDate");
                if (prop != null && prop.CanWrite)
                    prop.SetValue(booking, DateTime.UtcNow);
            }
            catch { }

            _db.TblEventBookings.Update(booking);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkBookingPaidAsync(int bookingId)
        {
            var booking = await _db.TblEventBookings.FindAsync(bookingId);
            if (booking == null) return false;

            booking.EventBookingPaymentStatus = 1;

            try
            {
                var type = booking.GetType();
                var paidAtProp = type.GetProperty("EventBookingPaidAt") ?? type.GetProperty("PaidAt");
                if (paidAtProp != null && paidAtProp.CanWrite)
                    paidAtProp.SetValue(booking, DateTime.UtcNow);

                var updatedProp = type.GetProperty("EventBookingUpdatedDate") ?? type.GetProperty("UpdatedAt");
                if (updatedProp != null && updatedProp.CanWrite)
                    updatedProp.SetValue(booking, DateTime.UtcNow);
            }
            catch { }

            _db.TblEventBookings.Update(booking);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelBookingAsync(int bookingId, int cancelStatus = 3)
        {
            var booking = await _db.TblEventBookings.FindAsync(bookingId);
            if (booking == null) return false;

            booking.EventBookingPaymentStatus = cancelStatus;
            try
            {
                var updatedProp = booking.GetType().GetProperty("EventBookingUpdatedDate") ?? booking.GetType().GetProperty("UpdatedAt");
                if (updatedProp != null && updatedProp.CanWrite)
                    updatedProp.SetValue(booking, DateTime.UtcNow);
            }
            catch { }

            _db.TblEventBookings.Update(booking);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetConfirmedSlotsForEventAsync(int eventId)
        {
            var confirmedStatuses = new[] { 1, 2 };
            var bookings = await _db.TblEventBookings
                .AsNoTracking()
                .Where(b => b.EventBookingEventId == eventId && b.EventBookingPaymentStatus != null && confirmedStatuses.Contains(b.EventBookingPaymentStatus.Value))
                .Select(b => new { b.EventBookingId, b.EventBookingNotes })
                .ToListAsync();

            int total = 0;
            foreach (var b in bookings)
            {
                int qty = ParseQtyFromNotes(b.EventBookingNotes);
                total += Math.Max(1, qty);
            }
            return total;
        }

        private int ParseQtyFromNotes(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return 1;

            try
            {
                var parts = notes.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    var t = p.Trim();
                    if (t.StartsWith("Qty", StringComparison.OrdinalIgnoreCase))
                    {
                        var sepIndex = t.IndexOfAny(new[] { ':', '=', ' ' });
                        string numberPart;
                        if (sepIndex >= 0 && sepIndex + 1 < t.Length)
                            numberPart = t.Substring(sepIndex + 1).Trim();
                        else if (sepIndex == -1 && t.Length > 3)
                            numberPart = t.Substring(3).Trim();
                        else
                            numberPart = "";

                        var digits = new string(numberPart.Where(c => char.IsDigit(c)).ToArray());
                        if (int.TryParse(digits, out var q) && q > 0) return q;

                        if (int.TryParse(numberPart, out var q2) && q2 > 0) return q2;
                    }
                }
            }
            catch { }

            return 1;
        }

        public async Task<TblEventBooking> GetByIdWithHistoryAsync(int id)
        {
            return await GetFullBookingQuery()
                .Include(b => b.TblEventBookingHistories.OrderByDescending(h => h.EventBookingHistoryCreatedAt))
                .FirstOrDefaultAsync(b => b.EventBookingId == id);
        }

        public async Task<bool> UpdateStatusByAdminAsync(int bookingId, int newStatus, int adminId)
        {
            var booking = await _db.TblEventBookings.FindAsync(bookingId);
            if (booking == null) return false;

            string oldStatusLabel = GetStatusLabel(booking.EventBookingPaymentStatus ?? 0);
            string newStatusLabel = GetStatusLabel(newStatus);

            booking.EventBookingPaymentStatus = newStatus;
            _db.TblEventBookings.Update(booking);

            var history = new TblEventBookingHistory
            {
                EventBookingHistoryBookingId = booking.EventBookingId,
                EventBookingHistoryEventId = booking.EventBookingEventId,
                EventBookingHistoryUserId = adminId, // always = 1 
                EventBookingHistoryAction = "UpdateStatus",
                EventBookingHistoryDetails = $"Status changed from '{oldStatusLabel}' to '{newStatusLabel}'",
                EventBookingHistoryCreatedAt = DateTime.UtcNow
            };
            _db.TblEventBookingHistories.Add(history);

            await _db.SaveChangesAsync();
            return true;
        }

        private string GetStatusLabel(int status)
        {
            return status switch
            {
                0 => "Unpaid",
                1 => "Paid",
                2 => "Free",
                3 => "Cancelled",
                _ => "Unknown"
            };
        }
    }
}
