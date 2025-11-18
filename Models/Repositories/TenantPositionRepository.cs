using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;

namespace Semester03.Models.Repositories
{
    // Concrete repository — inject AbcdmallContext qua constructor (DI)
    public class TenantPositionRepository
    {
        private readonly AbcdmallContext _db;

        public TenantPositionRepository(AbcdmallContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<List<TenantPositionDto>> GetPositionsByFloorAsync(int floor)
        {
            var list = await _db.TblTenantPositions
                .Where(p => p.TenantPositionFloor == floor)
                .OrderBy(p => p.TenantPositionId)
                .ToListAsync();

            return list.Select(p => new TenantPositionDto
            {
                TenantPosition_ID = p.TenantPositionId,
                TenantPosition_Location = p.TenantPositionLocation,
                TenantPosition_AssignedTenantID = p.TenantPositionAssignedTenantId,
                TenantPosition_Area_M2 = p.TenantPositionAreaM2,
                TenantPosition_Floor = p.TenantPositionFloor,
                TenantPosition_Status = p.TenantPositionStatus.HasValue ? (int)p.TenantPositionStatus : 0,
                TenantPosition_LeftPct = p.TenantPositionLeftPct,
                TenantPosition_TopPct = p.TenantPositionTopPct
            }).ToList();
        }

        public async Task<int> GetCountByFloorAsync(int floor)
        {
            return await _db.TblTenantPositions.CountAsync(p => p.TenantPositionFloor == floor);
        }

        public async Task<TenantPositionDto> GetByIdAsync(int id)
        {
            var p = await _db.TblTenantPositions.FindAsync(id);
            if (p == null) return null;

            return new TenantPositionDto
            {
                TenantPosition_ID = p.TenantPositionId,
                TenantPosition_Location = p.TenantPositionLocation,
                TenantPosition_AssignedTenantID = p.TenantPositionAssignedTenantId,
                TenantPosition_Area_M2 = p.TenantPositionAreaM2,
                TenantPosition_Floor = p.TenantPositionFloor,
                TenantPosition_Status = p.TenantPositionStatus.HasValue ? (int)p.TenantPositionStatus : 0,
                TenantPosition_LeftPct = p.TenantPositionLeftPct,
                TenantPosition_TopPct = p.TenantPositionTopPct
            };
        }

        public async Task DeleteAsync(int id)
        {
            var p = await _db.TblTenantPositions.FindAsync(id);
            if (p == null) return;

            _db.TblTenantPositions.Remove(p);
            await _db.SaveChangesAsync();
        }

        // Add/Create
        public async Task<int> CreateAsync(TblTenantPosition entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _db.TblTenantPositions.Add(entity);
            await _db.SaveChangesAsync();
            return entity.TenantPositionId;
        }

        // Update
        public async Task UpdateAsync(TblTenantPosition entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _db.TblTenantPositions.Update(entity);
            await _db.SaveChangesAsync();
        }

        // -----------------------
        // new: update position coords only
        public async Task<bool> UpdatePositionCoordsAsync(int id, decimal? leftPct, decimal? topPct)
        {
            var p = await _db.TblTenantPositions.FindAsync(id);
            if (p == null) return false;
            p.TenantPositionLeftPct = leftPct;
            p.TenantPositionTopPct = topPct;
            _db.TblTenantPositions.Update(p);
            await _db.SaveChangesAsync();
            return true;
        }

        // new: check if there exists another position on same floor too close to (leftPct,topPct)
        // minDistancePct is expressed in percentage units (0..100)
        public async Task<bool> HasNearbyPositionAsync(int id, decimal leftPct, decimal topPct, decimal minDistancePct = 6M)
        {
            // find the floor of the moving position
            var moving = await _db.TblTenantPositions.FindAsync(id);
            if (moving == null) return false; // no existing -> treat as no collision

            var floor = moving.TenantPositionFloor;

            // Use a simple approximate collision detection: squared-distance in percent units
            var thresholdSq = minDistancePct * minDistancePct;

            return await _db.TblTenantPositions
                .Where(p => p.TenantPositionFloor == floor && p.TenantPositionId != id && p.TenantPositionLeftPct != null && p.TenantPositionTopPct != null)
                .AnyAsync(p =>
                    ((p.TenantPositionLeftPct.Value - leftPct) * (p.TenantPositionLeftPct.Value - leftPct)
                    + (p.TenantPositionTopPct.Value - topPct) * (p.TenantPositionTopPct.Value - topPct)) < thresholdSq
                );
        }


        // ==========================================================
        // === ADMIN METHODS ===
        // ==========================================================

        public async Task<IEnumerable<TblTenantPosition>> GetAllAdminEntitiesAsync(string search = null, int? floor = null, int? status = null)
        {
            var query = _db.TblTenantPositions
                .Include(p => p.TenantPositionAssignedTenant) //
                .AsQueryable();

            // 1. Filter by Floor
            if (floor.HasValue)
            {
                query = query.Where(p => p.TenantPositionFloor == floor.Value);
            }

            // 2. Filter by Status (0=Vacant, 1=Occupied)
            if (status.HasValue)
            {
                query = query.Where(p => p.TenantPositionStatus == status.Value);
            }

            // 3. Search by Location Code OR Tenant Name
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(p =>
                    p.TenantPositionLocation.ToLower().Contains(search) ||
                    (p.TenantPositionAssignedTenant != null && p.TenantPositionAssignedTenant.TenantName.ToLower().Contains(search))
                );
            }

            return await query
                .OrderBy(p => p.TenantPositionFloor)
                .ThenBy(p => p.TenantPositionLocation)
                .ToListAsync();
        }

        public async Task<TblTenantPosition> GetEntityByIdAsync(int id)
        {
            return await _db.TblTenantPositions
                .Include(p => p.TenantPositionAssignedTenant)
                .FirstOrDefaultAsync(p => p.TenantPositionId == id);
        }
    }
}
