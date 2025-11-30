using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Partner.Models;
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
                .Include(b => b.EventBookingUser);
            // .Include(b => b.EventBookingTenant); // enable later if mapping ok
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

            // ===== Ensure Quantity =====
            int qty = 1;
            try
            {
                if (booking.EventBookingQuantity > 0)
                {
                    qty = (int)booking.EventBookingQuantity;
                }
                else
                {
                    qty = ParseQtyFromNotes(booking.EventBookingNotes);
                    if (qty <= 0) qty = 1;
                    booking.EventBookingQuantity = qty;
                }
            }
            catch
            {
                qty = ParseQtyFromNotes(booking.EventBookingNotes);
                if (qty <= 0) qty = 1;
            }

            // ===== Ensure UnitPrice =====
            try
            {
                if ((booking.EventBookingUnitPrice == null || booking.EventBookingUnitPrice <= 0) &&
                    booking.EventBookingTotalCost.HasValue)
                {
                    var total = booking.EventBookingTotalCost.Value;
                    var unitPrice = qty > 0 ? total / qty : total;
                    booking.EventBookingUnitPrice = unitPrice;
                }
            }
            catch { }

            // ===== Ensure Date =====
            try
            {
                if (booking.EventBookingDate == null ||
                    booking.EventBookingDate == default)
                {
                    booking.EventBookingDate = DateOnly.FromDateTime(DateTime.Now);
                }
            }
            catch { }

            _db.TblEventBookings.Add(booking);
            await _db.SaveChangesAsync();

            // History: CreatedBookingDay
            await AddHistoryAsync(
                booking.EventBookingId,
                booking.EventBookingEventId,
                booking.EventBookingUserId,
                "CreatedBookingDay",
                booking.EventBookingNotes,
                booking.EventBookingDate.HasValue
                    ? booking.EventBookingDate.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,
                TryGetQuantityFromBooking(booking)
            );

            return booking;
        }

        /// <summary>
        /// Simple create booking for event (client).
        /// </summary>
        public async Task<TblEventBooking> CreateBookingAsync(
            int tenantId,
            int? userId,
            int eventId,
            decimal totalCost,
            int quantity = 1,
            string notes = "")
        {
            var combinedNotes = notes ?? "";
            if (!combinedNotes.Contains("Qty:", StringComparison.OrdinalIgnoreCase))
            {
                combinedNotes = $"Qty:{quantity}" +
                                (string.IsNullOrWhiteSpace(combinedNotes) ? "" : ";" + combinedNotes);
            }

            var entity = new TblEventBooking
            {
                EventBookingTenantId = tenantId,
                EventBookingUserId = userId ?? 0,
                EventBookingEventId = eventId,
                EventBookingTotalCost = totalCost,
                EventBookingPaymentStatus = totalCost > 0m ? 0 : 2, // 0: pending, 2: free-confirmed
                EventBookingNotes = combinedNotes,
                EventBookingCreatedDate = DateTime.Now
            };

            // Quantity
            try
            {
                entity.EventBookingQuantity = quantity;
            }
            catch { }

            // UnitPrice
            try
            {
                var unitPrice = quantity > 0 ? totalCost / quantity : totalCost;
                entity.EventBookingUnitPrice = unitPrice;
            }
            catch { }

            // Date
            try
            {
                entity.EventBookingDate = DateOnly.FromDateTime(DateTime.Now);
            }
            catch { }

            _db.TblEventBookings.Add(entity);
            await _db.SaveChangesAsync();

            // History: CreatedBookingDay
            await AddHistoryAsync(
                entity.EventBookingId,
                entity.EventBookingEventId,
                entity.EventBookingUserId,
                "CreatedBookingDay",
                entity.EventBookingNotes,
                DateTime.Now.Date,
                quantity
            );

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
                    prop.SetValue(booking, DateTime.Now);
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

            booking.EventBookingPaymentStatus = 1; // Paid

            try
            {
                var type = booking.GetType();
                var paidAtProp = type.GetProperty("EventBookingPaidAt") ?? type.GetProperty("PaidAt");
                if (paidAtProp != null && paidAtProp.CanWrite)
                    paidAtProp.SetValue(booking, DateTime.Now);

                var updatedProp = type.GetProperty("EventBookingUpdatedDate") ?? type.GetProperty("UpdatedAt");
                if (updatedProp != null && updatedProp.CanWrite)
                    updatedProp.SetValue(booking, DateTime.Now);
            }
            catch { }

            _db.TblEventBookings.Update(booking);
            await _db.SaveChangesAsync();

            // History: PaymentSuccess
            await AddHistoryAsync(
                booking.EventBookingId,
                booking.EventBookingEventId,
                booking.EventBookingUserId,
                "PaymentSuccess",
                "Thanh toán thành công qua VNPAY",
                DateTime.Now.Date,
                TryGetQuantityFromBooking(booking)
            );

            return true;
        }

        public async Task<bool> CancelBookingAsync(int bookingId, int cancelStatus = 3)
        {
            var booking = await _db.TblEventBookings.FindAsync(bookingId);
            if (booking == null) return false;

            booking.EventBookingPaymentStatus = cancelStatus;
            booking.EventBookingStatus = 0;
            try
            {
                var updatedProp = booking.GetType().GetProperty("EventBookingUpdatedDate") ??
                                  booking.GetType().GetProperty("UpdatedAt");
                if (updatedProp != null && updatedProp.CanWrite)
                    updatedProp.SetValue(booking, DateTime.Now);
            }
            catch { }

            _db.TblEventBookings.Update(booking);
            await _db.SaveChangesAsync();

            // History: AdminCancelled
            await AddHistoryAsync(
                booking.EventBookingId,
                booking.EventBookingEventId,
                booking.EventBookingUserId,
                "AdminCancelled",
                "Đơn đặt vé bị hủy",
                DateTime.Now.Date,
                TryGetQuantityFromBooking(booking)
            );

            return true;
        }

        /// <summary>
        /// Count confirmed slots by SUM(EventBooking_Quantity) (status 1 or 2).
        /// Backward compatible with Notes when Quantity is null.
        /// </summary>
        public async Task<int> GetConfirmedSlotsForEventAsync(int eventId)
        {
            var confirmedStatuses = new[] { 1, 2 }; // 1 = Paid, 2 = Free / PartiallyRefunded

            var bookings = await _db.TblEventBookings
                .AsNoTracking()
                .Where(b => b.EventBookingEventId == eventId &&
                            b.EventBookingPaymentStatus != null &&
                            confirmedStatuses.Contains(b.EventBookingPaymentStatus.Value))
                .Select(b => new { b.EventBookingId, b.EventBookingQuantity, b.EventBookingNotes })
                .ToListAsync();

            int total = 0;

            foreach (var b in bookings)
            {
                int qty = b.EventBookingQuantity ?? 0;

                if (qty <= 0)
                {
                    qty = ParseQtyFromNotes(b.EventBookingNotes);
                }

                total += Math.Max(1, qty);
            }

            return total;
        }

        /// <summary>
        /// Check xem 1 user đã có booking confirmed (Paid / Free) cho 1 event chưa.
        /// Dùng để giới hạn 1 vé free / user / event.
        /// </summary>
        public async Task<bool> HasConfirmedBookingForUserAsync(int eventId, int userId)
        {
            var confirmedStatuses = new[] { 1, 2 }; // 1 = Paid, 2 = Free / PartiallyRefunded

            return await _db.TblEventBookings
                .AsNoTracking()
                .AnyAsync(b =>
                    b.EventBookingEventId == eventId &&
                    b.EventBookingUserId == userId &&
                    b.EventBookingPaymentStatus != null &&
                    confirmedStatuses.Contains(b.EventBookingPaymentStatus.Value));
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
            var booking = await GetFullBookingQuery()
                .Include(b => b.TblEventBookingHistories)
                .FirstOrDefaultAsync(b => b.EventBookingId == id);

            if (booking != null && booking.TblEventBookingHistories != null)
            {
                booking.TblEventBookingHistories = booking.TblEventBookingHistories
                    .OrderByDescending(h => h.EventBookingHistoryCreatedAt)
                    .ToList();
            }

            return booking;
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
                EventBookingHistoryUserId = adminId,
                EventBookingHistoryAction = "UpdateStatus",
                EventBookingHistoryDetails = $"Status changed from '{oldStatusLabel}' to '{newStatusLabel}'",
                EventBookingHistoryCreatedAt = DateTime.Now
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

        private int TryGetQuantityFromBooking(TblEventBooking booking)
        {
            try
            {
                if (booking.EventBookingQuantity > 0)
                    return (int)booking.EventBookingQuantity;
            }
            catch { }

            return ParseQtyFromNotes(booking.EventBookingNotes);
        }

        // ==========================
        // BOOKING HISTORY (Tbl_EventBookingHistory)
        // ==========================

        public async Task AddHistoryAsync(
            int? bookingId,
            int? eventId,
            int? userId,
            string action,
            string details = null,
            DateTime? relatedDate = null,
            int? quantity = null)
        {
            if (string.IsNullOrWhiteSpace(action)) action = "Unknown";

            var history = new TblEventBookingHistory
            {
                EventBookingHistoryBookingId = bookingId,
                EventBookingHistoryEventId = eventId,
                EventBookingHistoryUserId = userId,
                EventBookingHistoryAction = action,
                EventBookingHistoryDetails = details,
                EventBookingHistoryRelatedDate = relatedDate.HasValue
                    ? DateOnly.FromDateTime(relatedDate.Value)
                    : (DateOnly?)null,
                EventBookingHistoryQuantity = quantity,
                EventBookingHistoryCreatedAt = DateTime.Now
            };

            _db.TblEventBookingHistories.Add(history);
            await _db.SaveChangesAsync();
        }


        public async Task<IEnumerable<TblEventBooking>> SearchBookingsAsync(
            int? eventId,
            string keyword,
            DateTime? fromDate,
            DateTime? toDate,
            string status) // status: "paid", "unpaid", "cancelled", "all"
        {
            var query = GetFullBookingQuery();

            // 1. Filter by Event
            if (eventId.HasValue)
            {
                query = query.Where(b => b.EventBookingEventId == eventId);
            }

            // 2. Filter by Date Range (Booking Date)
            if (fromDate.HasValue)
            {
                query = query.Where(b => b.EventBookingCreatedDate >= fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                query = query.Where(b => b.EventBookingCreatedDate < toDate.Value.Date.AddDays(1));
            }

            // 3. Filter by Status
            if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
            {
                switch (status.ToLower())
                {
                    case "paid":
                        query = query.Where(b => b.EventBookingPaymentStatus == 1);
                        break;
                    case "unpaid":
                        query = query.Where(b => b.EventBookingPaymentStatus == 0);
                        break;
                    case "free":
                        query = query.Where(b => b.EventBookingPaymentStatus == 2);
                        break;
                    case "cancelled":
                        query = query.Where(b => b.EventBookingPaymentStatus == 3 || b.EventBookingStatus == 0);
                        break;
                }
            }

            // 4. Keyword Search (Customer Name, Phone, Email, BookingID)
            if (!string.IsNullOrEmpty(keyword))
            {
                string k = keyword.ToLower();
                query = query.Where(b =>
                    b.EventBookingId.ToString() == k ||
                    (b.EventBookingUser != null && (
                        b.EventBookingUser.UsersFullName.ToLower().Contains(k) ||
                        b.EventBookingUser.UsersPhone.Contains(k) ||
                        b.EventBookingUser.UsersEmail.ToLower().Contains(k)
                    ))
                );
            }

            return await query.OrderByDescending(b => b.EventBookingCreatedDate).ToListAsync();
        }
        //Partner
        public async Task<List<EventBooking>> GetAllBookingsByEventId(int eventId)
        {
            return await _db.TblEventBookings
                .Where(x => x.EventBookingEventId == eventId && x.EventBookingPaymentStatus == 1)
                .Select(x => new EventBooking
                {
                    Id = x.EventBookingId,
                    TenantId = x.EventBookingTenantId,
                    UserId = x.EventBookingUserId,
                    EventId = x.EventBookingEventId,
                    Date = (DateOnly)x.EventBookingDate,
                    Quantity = x.EventBookingQuantity ?? 0,
                    UnitPrice = (decimal)x.EventBookingUnitPrice,
                    TotalCost = (decimal)x.EventBookingTotalCost,
                    PaymentStatus = x.EventBookingPaymentStatus ?? 0,
                    Status = x.EventBookingStatus ?? 0,
                    Note = x.EventBookingNotes,
                    Created = x.EventBookingCreatedDate ?? DateTime.MinValue,
                    Username = x.EventBookingUser.UsersFullName
                })
                .ToListAsync();
        }
        public async Task<EventBooking?> FindById(int id)
        {
            return await _db.TblEventBookings
                .Where(t => t.EventBookingId == id)
                .Select(x => new EventBooking
                {
                    Id = x.EventBookingId,
                    TenantId = x.EventBookingTenantId,
                    UserId = x.EventBookingUserId,
                    EventId = x.EventBookingEventId,
                    Date = (DateOnly)x.EventBookingDate,
                    Quantity = x.EventBookingQuantity ?? 0,
                    UnitPrice = (decimal)x.EventBookingUnitPrice,
                    TotalCost = (decimal)x.EventBookingTotalCost,
                    PaymentStatus = x.EventBookingPaymentStatus ?? 0,
                    Status = x.EventBookingStatus ?? 0,
                    Note = x.EventBookingNotes,
                    Created = x.EventBookingCreatedDate ?? DateTime.MinValue,
                    //Username = x.EventBookingUser.UsersFullName,
                    //EventName = x.EventBookingEvent.EventName,
                    //TenantName = x.EventBookingTenant.TenantName
                })
                .FirstOrDefaultAsync();
        }
    }
}
