using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;

namespace Semester03.Models.Repositories
{
    public class EventBookingRepository
    {
        private readonly AbcdmallContext _db;

        public EventBookingRepository(AbcdmallContext db)
        {
            _db = db;
        }

        // Helper query to get all related data
        private IQueryable<TblEventBooking> GetFullBookingQuery()
        {
            return _db.TblEventBookings
                .Include(b => b.EventBookingEvent)    // Include Event
                .Include(b => b.EventBookingUser)     // Include User (Buyer)
                .Include(b => b.EventBookingTenant);  // Include Tenant (Host)
        }

        // --- ADMIN METHODS ---

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

        // Admin action to update payment status
        public async Task<bool> UpdatePaymentStatusAsync(int bookingId, int newStatus)
        {
            var booking = await _db.TblEventBookings.FindAsync(bookingId);
            if (booking == null)
            {
                return false;
            }

            booking.EventBookingPaymentStatus = newStatus;
            _db.TblEventBookings.Update(booking);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<TblEventBooking>> GetBookingsForEventAsync(int eventId)
        {
            // Use the same complex query from GetAllAsync
            return await GetFullBookingQuery()
                // Add a WHERE clause
                .Where(b => b.EventBookingEventId == eventId) //
                .OrderByDescending(b => b.EventBookingCreatedDate)
                .ToListAsync();
        }
    }
}
