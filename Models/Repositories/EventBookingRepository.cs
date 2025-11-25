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

        /// <summary>
        /// NOTE: We intentionally do NOT include EventBookingTenant here as a quick fix
        /// because including Tenant currently causes EF to generate a bad column name
        /// (TblTenantPositionTenantPositionId) — fix the model mapping in AbcdmallContext
        /// (see comments in code) and you can safely add Include(b => b.EventBookingTenant).
        /// </summary>
        private IQueryable<TblEventBooking> GetFullBookingQuery()
        {
            return _db.TblEventBookings
                .Include(b => b.EventBookingEvent)
                .Include(b => b.EventBookingUser);
            // .Include(b => b.EventBookingTenant); // <-- enable after fixing Tenant <-> TenantPosition mapping in DbContext
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

        /// <summary>
        /// Overload tạo booking từ entity có sẵn.
        /// Đảm bảo set: Quantity, UnitPrice, OrderGroup, Date nếu chưa có.
        /// </summary>
        public async Task<TblEventBooking> CreateBookingAsync(TblEventBooking booking)
        {
            if (booking == null) throw new ArgumentNullException(nameof(booking));

            // ====== ĐẢM BẢO Quantity ======
            int qty = 1;
            try
            {
                // nếu entity đã có EventBookingQuantity (scaffold từ DB)
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

            // ====== ĐẢM BẢO UnitPrice ======
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

            // ====== ĐẢM BẢO Date ======
            try
            {
                if (booking.EventBookingDate == null ||
                    booking.EventBookingDate == default)
                {
                    booking.EventBookingDate = DateOnly.FromDateTime(DateTime.UtcNow);
                }
            }
            catch { }

            // ====== ĐẢM BẢO OrderGroup (GUID) ======
            try
            {
                if (booking.EventBookingOrderGroup == null ||
                    booking.EventBookingOrderGroup == Guid.Empty)
                {
                    booking.EventBookingOrderGroup = Guid.NewGuid();
                }
            }
            catch { }

            _db.TblEventBookings.Add(booking);
            await _db.SaveChangesAsync();

            // Ghi lịch sử: CreatedBookingDay
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
        /// Tạo booking đơn giản cho Event (client đang dùng).
        /// totalCost: tổng tiền, quantity: số slot/vé.
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
                combinedNotes = $"Qty:{quantity}" + (string.IsNullOrWhiteSpace(combinedNotes) ? "" : ";" + combinedNotes);
            }

            var entity = new TblEventBooking
            {
                EventBookingTenantId = tenantId,
                EventBookingUserId = userId ?? 0,
                EventBookingEventId = eventId,
                EventBookingTotalCost = totalCost,
                EventBookingPaymentStatus = totalCost > 0m ? 0 : 2, // 0: pending, 2: free-confirmed
                EventBookingNotes = combinedNotes,
                EventBookingCreatedDate = DateTime.UtcNow
            };

            // ====== Quantity ======
            try
            {
                entity.EventBookingQuantity = quantity;
            }
            catch
            {
                // nếu entity không có property này thì bỏ qua
            }

            // ====== UnitPrice ======
            try
            {
                var unitPrice = quantity > 0 ? totalCost / quantity : totalCost;
                entity.EventBookingUnitPrice = unitPrice;
            }
            catch
            {
                // nếu không có cột thì thôi
            }

            // ====== Date ======
            try
            {
                entity.EventBookingDate = DateOnly.FromDateTime(DateTime.UtcNow);
            }
            catch
            {
                // nếu cột kiểu DateTime, bạn có thể đổi sang DateTime.UtcNow.Date
            }

            // ====== OrderGroup (GUID) ======
            try
            {
                entity.EventBookingOrderGroup = Guid.NewGuid();
            }
            catch
            {
                // nếu kiểu khác Guid thì bạn đổi cho phù hợp
            }

            _db.TblEventBookings.Add(entity);
            await _db.SaveChangesAsync();

            // Lịch sử: CreatedBookingDay
            await AddHistoryAsync(
                entity.EventBookingId,
                entity.EventBookingEventId,
                entity.EventBookingUserId,
                "CreatedBookingDay",
                entity.EventBookingNotes,
                DateTime.UtcNow.Date,
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

            booking.EventBookingPaymentStatus = 1; // 1 = Paid

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

            // Ghi history PaymentSuccess
            await AddHistoryAsync(
                booking.EventBookingId,
                booking.EventBookingEventId,
                booking.EventBookingUserId,
                "PaymentSuccess",
                "Thanh toán thành công qua VNPAY",
                DateTime.UtcNow.Date,
                TryGetQuantityFromBooking(booking)
            );

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

            // History: AdminCancelled hoặc Cancelled
            await AddHistoryAsync(
                booking.EventBookingId,
                booking.EventBookingEventId,
                booking.EventBookingUserId,
                "AdminCancelled",
                "Đơn đặt vé bị hủy",
                DateTime.UtcNow.Date,
                TryGetQuantityFromBooking(booking)
            );

            return true;
        }

        /// <summary>
        /// Tính tổng slot đã confirm (payment status 1 or 2).
        /// Dùng Qty trong notes (giữ backward-compatible).
        /// </summary>
        public async Task<int> GetConfirmedSlotsForEventAsync(int eventId)
        {
            var confirmedStatuses = new[] { 1, 2 };
            var bookings = await _db.TblEventBookings
                .AsNoTracking()
                .Where(b => b.EventBookingEventId == eventId &&
                            b.EventBookingPaymentStatus != null &&
                            confirmedStatuses.Contains(b.EventBookingPaymentStatus.Value))
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
        // LỊCH SỬ BOOKING (History)
        // ==========================

        /// <summary>
        /// Ghi 1 dòng vào Tbl_EventBookingHistory.
        /// Action có thể là:
        /// - CreatedBookingDay
        /// - PaymentSuccess
        /// - PaymentFailed
        /// - BookingUpdated
        /// - Refunded
        /// - AdminCancelled
        /// </summary>
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
                EventBookingHistoryRelatedDate = relatedDate.HasValue ? DateOnly.FromDateTime(relatedDate.Value) : (DateOnly?)null,
                EventBookingHistoryQuantity = quantity,
                EventBookingHistoryCreatedAt = DateTime.UtcNow
            };

            _db.TblEventBookingHistories.Add(history);
            await _db.SaveChangesAsync();
        }
    }
}
