using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Semester03.Models.Repositories
{
    public class TicketRepository
    {
        private readonly AbcdmallContext _db;
        public TicketRepository(AbcdmallContext db) => _db = db;

        // ===== Helper Include Query =====
        private IQueryable<TblTicket> GetFullTicketQuery()
        {
            return _db.TblTickets
                .Include(t => t.TicketBuyerUser)
                .Include(t => t.TicketShowtimeSeat)
                    .ThenInclude(ss => ss.ShowtimeSeatSeat)
                .Include(t => t.TicketShowtimeSeat)
                    .ThenInclude(ss => ss.ShowtimeSeatShowtime)
                        .ThenInclude(st => st.ShowtimeMovie)
                .Include(t => t.TicketShowtimeSeat)
                    .ThenInclude(ss => ss.ShowtimeSeatShowtime)
                        .ThenInclude(st => st.ShowtimeScreen);
        }

        // =======================================================
        //  ⭐ NEW — Get tickets by UserID
        // =======================================================
        public async Task<List<TblTicket>> GetTicketsByUserAsync(int userId)
        {
            return await GetFullTicketQuery()
                .Where(t => t.TicketBuyerUserId == userId)
                .OrderByDescending(t => t.TicketCreatedAt)
                .ToListAsync();
        }

        // =======================================================
        //  Existing — Used for Email
        // =======================================================
        public async Task<List<TicketEmailItem>> GetTicketDetailsByShowtimeSeatIdsAsync(List<int> showtimeSeatIds)
        {
            var result = await (from t in _db.TblTickets
                                join s in _db.TblShowtimeSeats on t.TicketShowtimeSeatId equals s.ShowtimeSeatId
                                join st in _db.TblShowtimes on s.ShowtimeSeatShowtimeId equals st.ShowtimeId
                                join m in _db.TblMovies on st.ShowtimeMovieId equals m.MovieId
                                join sc in _db.TblScreens on st.ShowtimeScreenId equals sc.ScreenId
                                join c in _db.TblCinemas on sc.ScreenCinemaId equals c.CinemaId
                                where showtimeSeatIds.Contains(s.ShowtimeSeatId)
                                select new TicketEmailItem
                                {
                                    MovieTitle = m.MovieTitle,
                                    CinemaName = c.CinemaName,
                                    ScreenName = sc.ScreenName,
                                    ShowtimeStart = st.ShowtimeStart,
                                    SeatLabel = s.ShowtimeSeatSeat.SeatLabel,
                                    Price = st.ShowtimePrice
                                }).ToListAsync();

            return result;
        }

        // =======================================================
        //  ADMIN METHODS (giữ nguyên)
        // =======================================================
        public async Task<IEnumerable<TblTicket>> GetAllAsync()
        {
            return await GetFullTicketQuery()
                .OrderByDescending(t => t.TicketCreatedAt)
                .ToListAsync();
        }

        public async Task<TblTicket> GetByIdAsync(int id)
        {
            return await GetFullTicketQuery()
                .FirstOrDefaultAsync(t => t.TicketId == id);
        }

        public async Task<int> CancelAllTicketsForShowtimeAsync(int showtimeId)
        {
            using (var transaction = await _db.Database.BeginTransactionAsync())
            {
                try
                {
                    var seatsToCancel = await _db.TblShowtimeSeats
                        .Where(ss => ss.ShowtimeSeatShowtimeId == showtimeId &&
                                     ss.ShowtimeSeatStatus.ToLower() == "sold")
                        .ToListAsync();

                    if (!seatsToCancel.Any())
                    {
                        await transaction.RollbackAsync();
                        return 0;
                    }

                    var seatIdsToCancel = seatsToCancel.Select(s => s.ShowtimeSeatId).ToList();

                    var ticketsToCancel = await _db.TblTickets
                        .Where(t => seatIdsToCancel.Contains(t.TicketShowtimeSeatId) &&
                                    t.TicketStatus.ToLower() == "sold")
                        .ToListAsync();

                    foreach (var ticket in ticketsToCancel)
                    {
                        ticket.TicketStatus = "cancelled";
                        _db.TblTickets.Update(ticket);
                    }

                    foreach (var seat in seatsToCancel)
                    {
                        seat.ShowtimeSeatStatus = "available";
                        seat.ShowtimeSeatReservedByUserId = null;
                        seat.ShowtimeSeatReservedAt = null;
                        _db.TblShowtimeSeats.Update(seat);
                    }

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return ticketsToCancel.Count;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return -1;
                }
            }
        }

        public async Task<IEnumerable<TblTicket>> GetTicketsForShowtimeAsync(int showtimeId)
        {
            return await GetFullTicketQuery()
                .Where(t => t.TicketShowtimeSeat.ShowtimeSeatShowtimeId == showtimeId)
                .OrderByDescending(t => t.TicketCreatedAt)
                .ToListAsync();
        }

        public async Task<bool> CancelTicketAsync(int ticketId)
        {
            using (var transaction = await _db.Database.BeginTransactionAsync())
            {
                try
                {
                    var ticket = await _db.TblTickets.FindAsync(ticketId);

                    if (ticket == null || ticket.TicketStatus.ToLower() != "sold")
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }

                    var showtimeSeat = await _db.TblShowtimeSeats.FindAsync(ticket.TicketShowtimeSeatId);
                    if (showtimeSeat == null)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }

                    ticket.TicketStatus = "cancelled";
                    _db.TblTickets.Update(ticket);

                    showtimeSeat.ShowtimeSeatStatus = "available";
                    showtimeSeat.ShowtimeSeatReservedByUserId = null;
                    showtimeSeat.ShowtimeSeatReservedAt = null;
                    _db.TblShowtimeSeats.Update(showtimeSeat);

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return true;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            }
        }
    }
}
