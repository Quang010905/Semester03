using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Semester03.Models.Repositories
{
    /// <summary>
    /// Repository xử lý TblEventBooking
    /// Assumptions:
    /// - EF entity is TblEventBooking with properties:
    ///   EventBookingId, EventBookingTenantId, EventBookingUserId, EventBookingEventId,
    ///   EventBookingTotalCost, EventBookingPaymentStatus, EventBookingNotes, EventBookingCreatedDate
    /// - Navigation properties: EventBookingEvent, EventBookingUser, EventBookingTenant
    /// - Quantity is stored inside EventBookingNotes as "Qty:N" (if DB has no Quantity column).
    /// </summary>
    public class EventBookingRepository
    {
        private readonly AbcdmallContext _db;

        public EventBookingRepository(AbcdmallContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // Include related navigation properties
        private IQueryable<TblEventBooking> GetFullBookingQuery()
        {
            return _db.TblEventBookings
                .Include(b => b.EventBookingEvent)
                .Include(b => b.EventBookingUser)
                .Include(b => b.EventBookingTenant);
        }

        // -----------------------
        // ADMIN / READ METHODS
        // -----------------------

        /// <summary>
        /// Get all bookings with related data (admin).
        /// </summary>
        public async Task<IEnumerable<TblEventBooking>> GetAllAsync()
        {
            return await GetFullBookingQuery()
                .OrderByDescending(b => b.EventBookingCreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get booking by id including navigation props.
        /// </summary>
        public async Task<TblEventBooking> GetByIdAsync(int id)
        {
            return await GetFullBookingQuery()
                .FirstOrDefaultAsync(b => b.EventBookingId == id);
        }

        /// <summary>
        /// Get bookings for a specific event (admin).
        /// </summary>
        public async Task<IEnumerable<TblEventBooking>> GetBookingsForEventAsync(int eventId)
        {
            return await GetFullBookingQuery()
                .Where(b => b.EventBookingEventId == eventId)
                .OrderByDescending(b => b.EventBookingCreatedDate)
                .ToListAsync();
        }

        // -----------------------
        // CREATE / UPDATE METHODS
        // -----------------------

        /// <summary>
        /// Create booking by passing an entity (useful if you already map fields).
        /// </summary>
        public async Task<TblEventBooking> CreateBookingAsync(TblEventBooking booking)
        {
            if (booking == null) throw new ArgumentNullException(nameof(booking));

            _db.TblEventBookings.Add(booking);
            await _db.SaveChangesAsync();
            return booking;
        }

        /// <summary>
        /// Convenience: create booking from fields.
        /// - quantity is stored inside EventBookingNotes (as "Qty:{quantity};...") because current DB lacks Quantity column.
        /// - totalCost: total amount for the booking (price * qty).
        /// - paymentStatus: 0 = pending, 1 = paid, 2 = free (we will set based on totalCost by default)
        /// </summary>
        public async Task<TblEventBooking> CreateBookingAsync(int tenantId, int? userId, int eventId, decimal totalCost, int quantity = 1, string notes = "")
        {
            // Build notes: ensure Qty is included
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
                EventBookingPaymentStatus = totalCost > 0m ? 0 : 2, // 0 = pending, 2 = free
                EventBookingNotes = combinedNotes,
                EventBookingCreatedDate = DateTime.UtcNow
            };

            _db.TblEventBookings.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// Update payment status by id (admin).
        /// </summary>
        public async Task<bool> UpdatePaymentStatusAsync(int bookingId, int newStatus)
        {
            var booking = await _db.TblEventBookings.FindAsync(bookingId);
            if (booking == null) return false;

            booking.EventBookingPaymentStatus = newStatus;
            // if you have UpdatedDate column, set it here (e.g. booking.EventBookingUpdatedDate = DateTime.UtcNow;)
            _db.TblEventBookings.Update(booking);
            await _db.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Mark a booking as paid (PaymentStatus = 1).
        /// </summary>
        public async Task<bool> MarkBookingPaidAsync(int bookingId)
        {
            var booking = await _db.TblEventBookings.FindAsync(bookingId);
            if (booking == null) return false;

            booking.EventBookingPaymentStatus = 1;
            _db.TblEventBookings.Update(booking);
            await _db.SaveChangesAsync();
            return true;
        }

        // -----------------------
        // CLIENT / QUERY HELPERS
        // -----------------------

        /// <summary>
        /// Get bookings for a specific user.
        /// </summary>
        public async Task<IEnumerable<TblEventBooking>> GetBookingsForUserAsync(int userId)
        {
            return await GetFullBookingQuery()
                .Where(b => b.EventBookingUserId == userId)
                .OrderByDescending(b => b.EventBookingCreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Count confirmed (occupied) slots for an event.
        /// Logic: consider PaymentStatus in {1,2} (paid or free). Parse Qty from EventBookingNotes as "Qty:N".
        /// If not present, fallback to 1.
        /// </summary>
        public async Task<int> GetConfirmedSlotsForEventAsync(int eventId)
        {
            var confirmedStatuses = new[] { 1, 2 };
            var bookings = await _db.TblEventBookings
                .AsNoTracking()
                .Where(b => b.EventBookingEventId == eventId && confirmedStatuses.Contains(b.EventBookingPaymentStatus ?? -1))
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

        /// <summary>
        /// Parse Qty from notes string. Accepts forms like:
        /// "Qty:3", "Qty: 2", "Qty=3", "Qty 3", or "Qty:3;ContactEmail:abc@x.com"
        /// Returns 1 if not found or parse fails.
        /// </summary>
        private int ParseQtyFromNotes(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return 1;

            try
            {
                // Split by ; and search for a part starting with Qty
                var parts = notes.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    var t = p.Trim();
                    if (t.StartsWith("Qty", StringComparison.OrdinalIgnoreCase))
                    {
                        // handle formats: "Qty:", "Qty=", "Qty "
                        var sepIndex = t.IndexOfAny(new[] { ':', '=', ' ' });
                        string numberPart;
                        if (sepIndex >= 0 && sepIndex + 1 < t.Length)
                            numberPart = t.Substring(sepIndex + 1).Trim();
                        else if (sepIndex == -1 && t.Length > 3) // maybe "Qty3"
                            numberPart = t.Substring(3).Trim();
                        else
                            numberPart = "";

                        // filter digits
                        var digits = new string(numberPart.Where(c => char.IsDigit(c)).ToArray());
                        if (int.TryParse(digits, out var q) && q > 0) return q;

                        // fallback: try parse full part
                        if (int.TryParse(numberPart, out var q2) && q2 > 0) return q2;
                    }
                }
            }
            catch
            {
                // ignore and fallback
            }

            return 1;
        }
    }
}
