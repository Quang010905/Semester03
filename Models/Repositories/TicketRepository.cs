using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;

namespace Semester03.Models.Repositories
{
    public class TicketRepository
    {
        private readonly AbcdmallContext _db;
        public TicketRepository(AbcdmallContext db) => _db = db;

        // Helper to build the complex query
        private IQueryable<TblTicket> GetFullTicketQuery()
        {
            return _db.TblTickets
                .Include(t => t.TicketBuyerUser) // Include User
                .Include(t => t.TicketShowtimeSeat) // Include ShowtimeSeat
                    .ThenInclude(ss => ss.ShowtimeSeatSeat) // Include Seat
                .Include(t => t.TicketShowtimeSeat)
                    .ThenInclude(ss => ss.ShowtimeSeatShowtime) // Include Showtime
                        .ThenInclude(st => st.ShowtimeMovie) // Include Movie
                .Include(t => t.TicketShowtimeSeat)
                    .ThenInclude(ss => ss.ShowtimeSeatShowtime)
                        .ThenInclude(st => st.ShowtimeScreen); // Include Screen
        }

        // --- ADMIN METHODS ---

        public async Task<IEnumerable<TblTicket>> GetAllAsync()
        {
            // Get all tickets, ordered by most recent first
            return await GetFullTicketQuery()
                .OrderByDescending(t => t.TicketCreatedAt)
                .ToListAsync();
        }

        public async Task<TblTicket> GetByIdAsync(int id)
        {
            // Get one specific ticket
            return await GetFullTicketQuery()
                .FirstOrDefaultAsync(t => t.TicketId == id);
        }

        public async Task<int> CancelAllTicketsForShowtimeAsync(int showtimeId)
        {
            // This method cancels all "sold" tickets for a showtime
            // and makes all "sold" seats available again.

            // Start a database transaction
            using (var transaction = await _db.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Find all "sold" ShowtimeSeat records for this showtime
                    var seatsToCancel = await _db.TblShowtimeSeats
                        .Where(ss => ss.ShowtimeSeatShowtimeId == showtimeId &&
                                     ss.ShowtimeSeatStatus.ToLower() == "sold")
                        .ToListAsync();

                    if (!seatsToCancel.Any())
                    {
                        // No tickets to cancel
                        await transaction.RollbackAsync();
                        return 0; // Return 0 (no tickets cancelled)
                    }

                    var seatIdsToCancel = seatsToCancel.Select(s => s.ShowtimeSeatId).ToList();

                    // 2. Find all "sold" Ticket records for these seats
                    var ticketsToCancel = await _db.TblTickets
                        .Where(t => seatIdsToCancel.Contains(t.TicketShowtimeSeatId) &&
                                    t.TicketStatus.ToLower() == "sold")
                        .ToListAsync();

                    // 3. Update all Tickets to "cancelled"
                    foreach (var ticket in ticketsToCancel)
                    {
                        ticket.TicketStatus = "cancelled";
                        _db.TblTickets.Update(ticket);
                    }

                    // 4. Update all ShowtimeSeats to "available"
                    foreach (var seat in seatsToCancel)
                    {
                        seat.ShowtimeSeatStatus = "available";
                        seat.ShowtimeSeatReservedByUserId = 0; // Clear the reservation
                        _db.TblShowtimeSeats.Update(seat);
                    }

                    // 5. Save all changes
                    await _db.SaveChangesAsync();

                    // 6. Commit the transaction
                    await transaction.CommitAsync();

                    // Return the number of tickets that were cancelled
                    return ticketsToCancel.Count;
                }
                catch (Exception)
                {
                    // If anything goes wrong, roll back
                    await transaction.RollbackAsync();
                    return -1; // Return -1 to indicate an error
                }
            }
        }

        public async Task<IEnumerable<TblTicket>> GetTicketsForShowtimeAsync(int showtimeId)
        {
            // Use the same complex query
            return await GetFullTicketQuery()
                // Add a WHERE clause
                .Where(t => t.TicketShowtimeSeat.ShowtimeSeatShowtimeId == showtimeId)
                .OrderByDescending(t => t.TicketCreatedAt)
                .ToListAsync();
        }

        public async Task<bool> CancelTicketAsync(int ticketId)
        {
            // Start a database transaction
            // This ensures BOTH tables update, or NEITHER does.
            using (var transaction = await _db.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Find the ticket to cancel
                    var ticket = await _db.TblTickets.FindAsync(ticketId);

                    if (ticket == null || ticket.TicketStatus.ToLower() != "sold")
                    {
                        // Can't cancel a ticket that isn't sold
                        await transaction.RollbackAsync();
                        return false;
                    }

                    // 2. Find the associated showtime seat
                    var showtimeSeat = await _db.TblShowtimeSeats.FindAsync(ticket.TicketShowtimeSeatId);

                    if (showtimeSeat == null)
                    {
                        // This should not happen, but it's a good safety check
                        await transaction.RollbackAsync();
                        return false;
                    }

                    // 3. Update the Ticket status
                    ticket.TicketStatus = "cancelled";
                    _db.TblTickets.Update(ticket);

                    // 4. Update the ShowtimeSeat status (make it available again)
                    showtimeSeat.ShowtimeSeatStatus = "available";
                    showtimeSeat.ShowtimeSeatReservedByUserId = 1; // Set to 1 (Admin/System user)
                    _db.TblShowtimeSeats.Update(showtimeSeat);

                    // 5. Save both changes at the same time
                    await _db.SaveChangesAsync();

                    // 6. Commit the transaction
                    await transaction.CommitAsync();

                    return true; // Success
                }
                catch (Exception)
                {
                    // If anything goes wrong, roll back all changes
                    await transaction.RollbackAsync();
                    return false; // Failure
                }
            }
        }
    }
}
