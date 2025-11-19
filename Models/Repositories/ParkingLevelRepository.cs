using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;

namespace Semester03.Models.Repositories
{
    public class ParkingLevelRepository
    {
        private readonly AbcdmallContext _db;
        public ParkingLevelRepository(AbcdmallContext db) => _db = db;

        // ==========================================================
        // ADMIN CRUD METHODS
        // ==========================================================

        public async Task<IEnumerable<TblParkingLevel>> GetAllAsync()
        {
            // Get all levels, ordered by name
            return await _db.TblParkingLevels
                .OrderBy(l => l.LevelName)
                .ToListAsync();
        }

        public async Task<TblParkingLevel> GetByIdAsync(int id)
        {
            // Used for Edit/Delete(GET)
            return await _db.TblParkingLevels.FindAsync(id);
        }

        public async Task<TblParkingLevel> GetByIdWithSpotsAsync(int id)
        {
            // Used for the "All-in-One" Details page 
            return await _db.TblParkingLevels
                .Include(l => l.TblParkingSpots
                                .OrderBy(s => s.SpotRow)
                                .ThenBy(s => s.SpotCol))
                .FirstOrDefaultAsync(l => l.LevelId == id);
        }

        public async Task<TblParkingLevel> AddAsync(TblParkingLevel entity)
        {
            _db.TblParkingLevels.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(TblParkingLevel entity)
        {
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.TblParkingLevels.FindAsync(id);
            if (entity != null)
            {
                _db.TblParkingLevels.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }
    }
}
