using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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

        /// <summary>
        /// Check availability for given showtimeSeatIds. This method DOES NOT update database.
        /// It returns succeeded = ids that are available, failed = ids that are not available or not found.
        /// </summary>
        public (List<int> succeeded, List<int> failed) ReserveSeats(int showtimeId, List<int> showtimeSeatIds, int? userId)
        {
            var succeeded = new List<int>();
            var failed = new List<int>();

            if (showtimeSeatIds == null || !showtimeSeatIds.Any())
                return (succeeded, failed);

            // Find existing showtime-seat rows that are considered available
            var available = _db.TblShowtimeSeats
                .AsNoTracking()
                .Where(ss => ss.ShowtimeSeatShowtimeId == showtimeId
                             && showtimeSeatIds.Contains(ss.ShowtimeSeatId)
                             && (ss.ShowtimeSeatStatus == null
                                 || ss.ShowtimeSeatStatus.Trim() == ""
                                 || ss.ShowtimeSeatStatus.ToLower() == "available"
                                 || ss.ShowtimeSeatStatus.ToLower() == "free"))
                .Select(ss => ss.ShowtimeSeatId)
                .ToList();

            succeeded = available;
            failed = showtimeSeatIds.Except(available).ToList();

            return (succeeded, failed);
        }

        /// <summary>
        /// Finalize seats AFTER successful payment. This method will atomically:
        ///  - update Tbl_ShowtimeSeat status -> 'sold'
        ///  - insert corresponding Tbl_Ticket rows for each updated seat
        /// Returns (succeededIds, failedIds).
        /// </summary>
        public (List<int> succeeded, List<int> failed) FinalizeSeatsAtomic(int showtimeId, List<int> showtimeSeatIds, int? userId, decimal pricePerSeat)
        {
            var succeeded = new List<int>();
            var failed = new List<int>();

            if (showtimeSeatIds == null || !showtimeSeatIds.Any())
                return (succeeded, showtimeSeatIds ?? new List<int>());

            // Build parameter placeholders
            var paramNames = new List<string>();
            for (int i = 0; i < showtimeSeatIds.Count; i++)
                paramNames.Add($"@p{i}");

            var sql = $@"
DECLARE @Updated TABLE (Id INT);

UPDATE dbo.Tbl_ShowtimeSeat
SET ShowtimeSeat_Status = 'sold',
    ShowtimeSeat_ReservedByUserID = CASE WHEN @userId IS NOT NULL THEN @userId ELSE ShowtimeSeat_ReservedByUserID END,
    ShowtimeSeat_ReservedAt = SYSUTCDATETIME(),
    ShowtimeSeat_UpdatedAt = SYSUTCDATETIME()
OUTPUT INSERTED.ShowtimeSeat_ID INTO @Updated(Id)
WHERE ShowtimeSeat_ShowtimeID = @showtimeId
  AND ShowtimeSeat_ID IN ({string.Join(",", paramNames)})
  AND (
        ISNULL(LTRIM(RTRIM(ShowtimeSeat_Status)), '') = ''
        OR LOWER(LTRIM(RTRIM(ShowtimeSeat_Status))) = 'available'
        OR LOWER(LTRIM(RTRIM(ShowtimeSeat_Status))) = 'free'
      );

-- Insert tickets for successfully updated showtime seats
INSERT INTO dbo.Tbl_Ticket (Ticket_ShowtimeID, Ticket_ShowtimeSeatID, Ticket_BuyerUserID, Ticket_Status, Ticket_Price, Ticket_PurchasedAt)
SELECT ss.ShowtimeSeat_ShowtimeID, ss.ShowtimeSeat_ID, @userId, 'sold', @price, SYSUTCDATETIME()
FROM dbo.Tbl_ShowtimeSeat ss
JOIN @Updated u ON u.Id = ss.ShowtimeSeat_ID;

-- return updated ids
SELECT Id FROM @Updated;
";

            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) conn.Open();

            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;

            // parameters
            var pShowtime = cmd.CreateParameter();
            pShowtime.ParameterName = "@showtimeId";
            pShowtime.Value = showtimeId;
            cmd.Parameters.Add(pShowtime);

            var pUser = cmd.CreateParameter();
            pUser.ParameterName = "@userId";
            pUser.Value = (object)userId ?? DBNull.Value;
            cmd.Parameters.Add(pUser);

            var pPrice = cmd.CreateParameter();
            pPrice.ParameterName = "@price";
            pPrice.Value = pricePerSeat;
            cmd.Parameters.Add(pPrice);

            for (int i = 0; i < showtimeSeatIds.Count; i++)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = $"@p{i}";
                p.Value = showtimeSeatIds[i];
                cmd.Parameters.Add(p);
            }

            try
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        succeeded.Add(Convert.ToInt32(reader.GetValue(0)));
                    }
                }

                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { /* ignore rollback error */ }
                throw;
            }
            finally
            {
                if (conn.State == ConnectionState.Open) conn.Close();
            }

            failed = showtimeSeatIds.Except(succeeded).ToList();
            return (succeeded, failed);
        }


    }
}
