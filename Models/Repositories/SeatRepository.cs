using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;

namespace Semester03.Models.Repositories
{
    public class SeatRepository
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
                        ss.ShowtimeSeatReservedAt,
                        s.SeatIsActive // <-- thêm cột Seat_IsActive từ Tbl_Seat
                    };

            var list = q.OrderBy(x => x.SeatRow).ThenBy(x => x.SeatCol).ToList();

            var seats = list.Select(x => new SeatVm
            {
                ShowtimeSeatId = x.ShowtimeSeatId,
                Label = x.SeatLabel ?? "",
                Row = x.SeatRow ?? "",
                Col = x.SeatCol,
                Status = string.IsNullOrEmpty(x.ShowtimeSeatStatus) ? "available" : x.ShowtimeSeatStatus,
                ReservedByUserId = x.ShowtimeSeatReservedByUserId,
                ReservedAt = x.ShowtimeSeatReservedAt,
                IsActive = x.SeatIsActive ?? true 
            }).ToList();

            var maxCol = seats.Any() ? seats.Max(s => s.Col) : 10;

            return new SelectSeatVm
            {
                ShowtimeId = showtimeId,
                MovieTitle = st.ShowtimeMovie?.MovieTitle ?? "",
                ScreenName = st.ShowtimeScreen?.ScreenName ?? "",
                ShowtimeStart = st.ShowtimeStart,
                Seats = seats,
                MaxCols = Math.Max(10, maxCol)
            };
        }

        public SelectSeatVm RefreshSeatLayout(int showtimeId) => GetSeatLayoutForShowtime(showtimeId);

        public (List<int> succeeded, List<int> failed) ReserveSeats(int showtimeId, List<int> showtimeSeatIds, int? userId)
        {
            var succeeded = new List<int>();
            var failed = new List<int>();

            if (showtimeSeatIds == null || !showtimeSeatIds.Any())
                return (succeeded, failed);

            // Lấy danh sách available **và** chỉ những ghế có Seat_IsActive = 1 (không hư)
            var available = _db.TblShowtimeSeats
                .AsNoTracking()
                .Where(ss => ss.ShowtimeSeatShowtimeId == showtimeId
                             && showtimeSeatIds.Contains(ss.ShowtimeSeatId)
                             && (ss.ShowtimeSeatStatus == null
                                 || ss.ShowtimeSeatStatus.Trim() == ""
                                 || ss.ShowtimeSeatStatus.ToLower() == "available"
                                 || ss.ShowtimeSeatStatus.ToLower() == "free"))
                .Join(_db.TblSeats.AsNoTracking(),
                      ss => ss.ShowtimeSeatSeatId,
                      s => s.SeatId,
                      (ss, s) => new { ss.ShowtimeSeatId, SeatIsActive = s.SeatIsActive })
                .Where(x => x.SeatIsActive == true) // CHẶN những ghế đang hư (SeatIsActive == false)
                .Select(x => x.ShowtimeSeatId)
                .ToList();

            succeeded = available;
            failed = showtimeSeatIds.Except(available).ToList();

            return (succeeded, failed);
        }

        public (List<int> succeeded, List<int> failed) FinalizeSeatsAtomic(int showtimeId, List<int> showtimeSeatIds, int? userId, decimal pricePerSeat)
        {
            var succeeded = new List<int>();
            var failed = new List<int>();

            if (showtimeSeatIds == null || !showtimeSeatIds.Any())
                return (succeeded, showtimeSeatIds ?? new List<int>());

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

INSERT INTO dbo.Tbl_Ticket (Ticket_ShowtimeID, Ticket_ShowtimeSeatID, Ticket_BuyerUserID, Ticket_Status, Ticket_Price, Ticket_PurchasedAt)
SELECT ss.ShowtimeSeat_ShowtimeID, ss.ShowtimeSeat_ID, @userId, 'sold', @price, SYSUTCDATETIME()
FROM dbo.Tbl_ShowtimeSeat ss
JOIN @Updated u ON u.Id = ss.ShowtimeSeat_ID;

SELECT Id FROM @Updated;
";

            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) conn.Open();

            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;

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
                try { tx.Rollback(); } catch { }
                throw;
            }
            finally
            {
                if (conn.State == ConnectionState.Open) conn.Close();
            }

            failed = showtimeSeatIds.Except(succeeded).ToList();
            return (succeeded, failed);
        }

        // ADMIN CRUD METHODS 
        // ==========================================================

        public async Task<IEnumerable<TblSeat>> GetAllAsync()
        {
            // We Include() the Screen to show its name in the Admin list
            return await _db.TblSeats
                .Include(s => s.SeatScreen) //
                .ToListAsync();
        }

        public async Task<TblSeat> GetByIdAsync(int id)
        {
            return await _db.TblSeats.FindAsync(id);
        }

        public async Task BatchAddAsync(List<TblSeat> seats)
        {
            // AddRange is much faster than Add() in a loop
            await _db.TblSeats.AddRangeAsync(seats);
            await _db.SaveChangesAsync();
        }

        public async Task<TblSeat> AddAsync(TblSeat entity)
        {
            _db.TblSeats.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(TblSeat entity)
        {
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.TblSeats.FindAsync(id);
            if (entity != null)
            {
                _db.TblSeats.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<bool> CheckCollisionAsync(int screenId, string row, int col, int currentSeatId = 0)
        {
            // Check if any *other* seat (Id != currentSeatId) 
            // in *this* screen
            // already has the same Row and Col
            return await _db.TblSeats.AnyAsync(s =>
                s.SeatScreenId == screenId &&
                s.SeatRow == row &&
                s.SeatCol == col &&
                s.SeatId != currentSeatId // Ignore itself when editing
            );
        }
    }
}
