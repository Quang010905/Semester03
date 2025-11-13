using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;

namespace Semester03.Models.Repositories
{
    public class ScreenRepository
    {
        private readonly AbcdmallContext _db;

        // 1. Use Dependency Injection, just like the other repositories
        public ScreenRepository(AbcdmallContext db)
        {
            _db = db;
        }

        // ==========================================================
        // ADMIN CRUD METHODS
        // ==========================================================

        public async Task<IEnumerable<TblScreen>> GetAllAsync()
        {
            // We use Include() to get the Cinema's name for the Index page
            return await _db.TblScreens.Include(s => s.ScreenCinema).ToListAsync();
        }

        public async Task<TblScreen> GetByIdAsync(int id)
        {
            return await _db.TblScreens.FindAsync(id);
        }

        public async Task<TblScreen> GetByIdWithDetailsAsync(int id)
        {
            // This specific method loads the related data needed for the Details page
            return await _db.TblScreens
                .Include(s => s.ScreenCinema) // Load the Cinema Name
                .Include(s => s.TblSeats)     // Load the list of Seats
                .FirstOrDefaultAsync(m => m.ScreenId == id);
        }

        public async Task<TblScreen> AddAsync(TblScreen entity)
        {
            _db.TblScreens.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(TblScreen entity)
        {
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.TblScreens.FindAsync(id);
            if (entity != null)
            {
                _db.TblScreens.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }
    }
}
