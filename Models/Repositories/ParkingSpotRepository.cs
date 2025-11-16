using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;

namespace Semester03.Models.Repositories
{
    public class ParkingSpotRepository
    {
        private readonly AbcdmallContext _db;
        public ParkingSpotRepository(AbcdmallContext db) => _db = db;

        // ==========================================================
        // ADMIN CRUD METHODS (Copied from SeatRepository)
        // ==========================================================

        public async Task<TblParkingSpot> GetByIdAsync(int id)
        {
            return await _db.TblParkingSpots.FindAsync(id);
        }

        public async Task<TblParkingSpot> AddAsync(TblParkingSpot entity)
        {
            _db.TblParkingSpots.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(TblParkingSpot entity)
        {
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.TblParkingSpots.FindAsync(id);
            if (entity != null)
            {
                _db.TblParkingSpots.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }

        // Batch create
        public async Task BatchAddAsync(List<TblParkingSpot> spots)
        {
            await _db.TblParkingSpots.AddRangeAsync(spots);
            await _db.SaveChangesAsync();
        }

        // Collision check
        public async Task<bool> CheckCollisionAsync(int levelId, string row, int col, int currentSpotId = 0)
        {
            return await _db.TblParkingSpots.AnyAsync(s =>
                s.SpotLevelId == levelId &&
                s.SpotRow == row &&
                s.SpotCol == col &&
                s.ParkingSpotId != currentSpotId // Ignore itself when editing
            );
        }
    }
}
