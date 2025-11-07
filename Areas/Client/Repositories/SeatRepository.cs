using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;

namespace Semester03.Areas.Client.Repositories
{
    public class SeatRepository : ISeatRepository
    {
        private readonly AbcdmallContext _db;
        public SeatRepository(AbcdmallContext db) => _db = db;

        public SelectSeatVm GetSeatLayoutForShowtime(int showtimeId)
        {
            var st = _db.TblShowtimes
                        .AsNoTracking()
                        .Include(s => s.ShowtimeMovie)
                        .Include(s => s.ShowtimeScreen)
                        .FirstOrDefault(s => s.ShowtimeId == showtimeId);

            if (st == null) return new SelectSeatVm { ShowtimeId = showtimeId, Seats = new List<SeatVm>() };

            // get seat statuses for that showtime, join seat definition
            var q = from ss in _db.TblShowtimeSeats.AsNoTracking()
                    join s in _db.TblSeats.AsNoTracking() on ss.ShowtimeSeatSeatId equals s.SeatId
                    where ss.ShowtimeSeatShowtimeId == showtimeId
                    select new
                    {
                        ss.ShowtimeSeatId,
                        s.SeatLabel,
                        s.SeatRow,
                        s.SeatCol,
                        ss.ShowtimeSeatStatus,
                        ss.ShowtimeSeatReservedByUserId,
                        ss.ShowtimeSeatReservedAt
                    };

            var list = q.OrderBy(x => x.SeatRow).ThenBy(x => x.SeatCol).ToList();

            var seats = list.Select(x => new SeatVm
            {
                ShowtimeSeatId = x.ShowtimeSeatId,
                Label = x.SeatLabel,
                Row = x.SeatRow ?? "",
                Col = x.SeatCol ?? 0,
                Status = string.IsNullOrEmpty(x.ShowtimeSeatStatus) ? "available" : x.ShowtimeSeatStatus,
                ReservedByUserId = x.ShowtimeSeatReservedByUserId,
                ReservedAt = x.ShowtimeSeatReservedAt
            }).ToList();

            // compute max columns to help layout
            var maxCol = seats.Any() ? seats.Max(s => s.Col) : 10;

            return new SelectSeatVm
            {
                ShowtimeId = showtimeId,
                MovieTitle = st.ShowtimeMovie?.MovieTitle ?? "",
                ScreenName = st.ShowtimeScreen?.ScreenName ?? "",
                ShowtimeStart = st.ShowtimeStart,
                Seats = seats,
                MaxCols = maxCol
            };
        }

        public SelectSeatVm RefreshSeatLayout(int showtimeId) => GetSeatLayoutForShowtime(showtimeId);

        public (List<int> succeeded, List<int> failed) ReserveSeats(int showtimeId, List<int> showtimeSeatIds, int? userId)
        {
            var succeeded = new List<int>();
            var failed = new List<int>();

            using var tx = _db.Database.BeginTransaction();
            try
            {
                // lock rows for update: SELECT ... FOR UPDATE not available in SQL Server via EF Core,
                // but we can just re-query and update where status = 'available'.
                var seats = _db.TblShowtimeSeats
                               .Where(ss => ss.ShowtimeSeatShowtimeId == showtimeId && showtimeSeatIds.Contains(ss.ShowtimeSeatId))
                               .ToList();

                foreach (var s in seats)
                {
                    if (string.Equals(s.ShowtimeSeatStatus, "available", StringComparison.OrdinalIgnoreCase))
                    {
                        s.ShowtimeSeatStatus = "reserved";
                        s.ShowtimeSeatReservedByUserId = userId;
                        s.ShowtimeSeatReservedAt = DateTime.Now;
                        s.ShowtimeSeatUpdatedAt = DateTime.Now;
                        succeeded.Add(s.ShowtimeSeatId);
                    }
                    else
                    {
                        failed.Add(s.ShowtimeSeatId);
                    }
                }

                _db.SaveChanges();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                // on exception, consider everything failed
                failed.AddRange(showtimeSeatIds.Except(succeeded));
            }

            return (succeeded, failed);
        }
    }
}
