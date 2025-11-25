using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;

namespace Semester03.Models.Repositories
{
    public class ShowtimeRepository
    {
        private readonly AbcdmallContext _db;
        public ShowtimeRepository(AbcdmallContext db) => _db = db;

        public List<ShowtimeVm> GetShowtimesForMovieOnDate(int movieId, DateTime localDate)
        {
            var start = localDate.Date;
            var end = start.AddDays(1);

            var q = _db.TblShowtimes
                       .AsNoTracking()
                       .Include(s => s.ShowtimeScreen)
                           .ThenInclude(sc => sc.ScreenCinema)
                       .Where(s => s.ShowtimeMovieId == movieId
                                   && s.ShowtimeStart >= start
                                   && s.ShowtimeStart < end)
                       .OrderBy(s => s.ShowtimeStart)
                       .Select(s => new ShowtimeVm
                       {
                           Id = s.ShowtimeId,
                           StartTime = s.ShowtimeStart,
                           Price = s.ShowtimePrice,
                           ScreenName = s.ShowtimeScreen != null ? s.ShowtimeScreen.ScreenName : "",
                           CinemaName = s.ShowtimeScreen != null && s.ShowtimeScreen.ScreenCinema != null
                                        ? s.ShowtimeScreen.ScreenCinema.CinemaName : ""
                       });

            return q.ToList();
        }

        public IEnumerable<DateTime> GetAvailableDatesForMovie(int movieId, DateTime fromDate, int days)
        {
            var end = fromDate.Date.AddDays(days);
            var q = _db.TblShowtimes
                       .AsNoTracking()
                       .Where(s => s.ShowtimeMovieId == movieId
                                   && s.ShowtimeStart >= fromDate.Date
                                   && s.ShowtimeStart < end)
                       .Select(s => s.ShowtimeStart.Date)
                       .Distinct()
                       .OrderBy(d => d);

            var result = new List<DateTime>();
            for (int i = 0; i < days; i++)
                result.Add(fromDate.Date.AddDays(i));

            return result;
        }

        // ==========================================================
        // ADMIN CRUD METHODS 
        // ==========================================================

        public async Task<IEnumerable<TblShowtime>> GetAllAsync()
        {
            // We MUST Include() Movie and Screen to display their names
            return await _db.TblShowtimes
                .Include(s => s.ShowtimeMovie)
                .Include(s => s.ShowtimeScreen)
                .ToListAsync();
        }

        public async Task<TblShowtime> GetByIdAsync(int id)
        {
            return await _db.TblShowtimes
                .Include(s => s.ShowtimeMovie)
                .Include(s => s.ShowtimeScreen)
                .Include(s => s.TblShowtimeSeats)
                    .ThenInclude(ss => ss.ShowtimeSeatSeat)
                .FirstOrDefaultAsync(s => s.ShowtimeId == id);
        }

        public async Task<TblShowtime> AddAsync(TblShowtime entity)
        {
            // 1. Save the Showtime first to get the ID
            _db.TblShowtimes.Add(entity);
            await _db.SaveChangesAsync();

            // 2. === AUTO-GENERATE SEATS ===
            // Find all physical seats for this screen
            var physicalSeats = await _db.TblSeats
                .Where(s => s.SeatScreenId == entity.ShowtimeScreenId)
                .ToListAsync();

            // Create a ShowtimeSeat record for each physical seat
            var showtimeSeats = physicalSeats.Select(seat => new TblShowtimeSeat
            {
                ShowtimeSeatShowtimeId = entity.ShowtimeId,
                ShowtimeSeatSeatId = seat.SeatId,
                ShowtimeSeatStatus = "available", // Default status
                ShowtimeSeatUpdatedAt = DateTime.Now
            }).ToList();

            await _db.TblShowtimeSeats.AddRangeAsync(showtimeSeats);
            await _db.SaveChangesAsync();
            // ==============================

            return entity;
        }

        public async Task UpdateAsync(TblShowtime entity)
        {
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.TblShowtimes.FindAsync(id);
            if (entity != null)
            {
                var seats = _db.TblShowtimeSeats.Where(s => s.ShowtimeSeatShowtimeId == id);
                var seatIds = seats.Select(s => s.ShowtimeSeatId).ToList();

                var cancelledTickets = _db.TblTickets.Where(t => seatIds.Contains(t.TicketShowtimeSeatId));
                _db.TblTickets.RemoveRange(cancelledTickets);

                _db.TblShowtimeSeats.RemoveRange(seats);

                _db.TblShowtimes.Remove(entity);

                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<TblShowtime>> GetShowtimesByDateAsync(DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = date.Date.AddDays(1);

            return await _db.TblShowtimes
                .Include(s => s.ShowtimeMovie)
                .Include(s => s.ShowtimeScreen)
                .Include(s => s.TblShowtimeSeats) // Needed for occupancy calc
                .Where(s => s.ShowtimeStart >= startOfDay && s.ShowtimeStart < endOfDay)
                .OrderBy(s => s.ShowtimeStart)
                .ToListAsync();
        }

        public async Task<List<TblScreen>> GetAllScreensAsync()
        {
            return await _db.TblScreens.OrderBy(s => s.ScreenName).ToListAsync();
        }

        // Check if the new showtime overlaps with existing ones
        // cleaningBufferMinutes: Buffer time between shows (e.g., 30 mins)
        public async Task<bool> CheckOverlapAsync(int screenId, DateTime newStart, int movieDurationMin, int? excludeShowtimeId = null, int cleaningBufferMinutes = 30)
        {
            // Calculate the end time of the new show (including cleaning buffer)
            // New End (Physical) = Start + Duration
            // New End (Effective) = Start + Duration + Buffer
            var newEndWithBuffer = newStart.AddMinutes(movieDurationMin + cleaningBufferMinutes);

            // Get all showtimes for that screen on that day
            // (Preliminary filter to reduce DB load)
            var dateCheck = newStart.Date;

            var existingShows = await _db.TblShowtimes
                .AsNoTracking()
                .Include(s => s.ShowtimeMovie)
                .Where(s => s.ShowtimeScreenId == screenId
                            && s.ShowtimeStart >= dateCheck.AddDays(-1) // Check today and yesterday (in case late-night shows spill over)
                            && s.ShowtimeStart <= dateCheck.AddDays(1))
                .ToListAsync();

            foreach (var existing in existingShows)
            {
                // Skip itself (if editing)
                if (excludeShowtimeId.HasValue && existing.ShowtimeId == excludeShowtimeId.Value)
                    continue;

                // Calculate existing show duration
                var existingStart = existing.ShowtimeStart;
                var existingEndWithBuffer = existing.ShowtimeStart.AddMinutes(existing.ShowtimeMovie.MovieDurationMin + cleaningBufferMinutes);

                // OVERLAP CHECK LOGIC:
                // (Start A < End B) AND (End A > Start B)
                // A is New Show, B is Existing Show
                if (newStart < existingEndWithBuffer && newEndWithBuffer > existingStart)
                {
                    return true; // OVERLAP DETECTED
                }
            }

            return false; // NO OVERLAP (Safe)
        }

        // Helper to get movie duration (needed for Controller)
        public async Task<int> GetMovieDurationAsync(int movieId)
        {
            var movie = await _db.TblMovies.FindAsync(movieId);
            return movie?.MovieDurationMin ?? 0;
        }

    }
}
