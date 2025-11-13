using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;

namespace Semester03.Models.Repositories
{
    public class CouponRepository
    {
        private readonly AbcdmallContext _db;
        public CouponRepository(AbcdmallContext db) => _db = db;

        // ==========================================================
        // ADMIN CRUD METHODS
        // ==========================================================

        public async Task<IEnumerable<TblCoupon>> GetAllAsync()
        {
            return await _db.TblCoupons.ToListAsync();
        }

        public async Task<TblCoupon> GetByIdAsync(int id)
        {
            return await _db.TblCoupons.FindAsync(id);
        }

        public async Task<TblCoupon> AddAsync(TblCoupon entity)
        {
            _db.TblCoupons.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(TblCoupon entity)
        {
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.TblCoupons.FindAsync(id);
            if (entity != null)
            {
                _db.TblCoupons.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }
    }
}
