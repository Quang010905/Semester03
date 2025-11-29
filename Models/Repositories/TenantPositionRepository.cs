using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Areas.Partner.Models;
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
            // use AsNoTracking for read-only queries
            var list = await _db.TblTenantPositions
                .AsNoTracking()
                .Where(p => p.TenantPositionFloor == floor)
                .OrderBy(p => p.TenantPositionId)
                .ToListAsync();

            if (list == null || !list.Any()) return new List<TenantPositionDto>();

            return list.Select(p => new TenantPositionDto
            {
                TenantPosition_ID = p.TenantPositionId,
                TenantPosition_Location = p.TenantPositionLocation,
                TenantPosition_AssignedTenantID = p.TenantPositionAssignedTenantId,
                TenantPosition_Area_M2 = p.TenantPositionAreaM2,
                TenantPosition_Floor = p.TenantPositionFloor,
                TenantPosition_Status = p.TenantPositionStatus.HasValue ? (int)p.TenantPositionStatus : 0,
                TenantPosition_LeftPct = p.TenantPositionLeftPct,
                TenantPosition_TopPct = p.TenantPositionTopPct,
                Tenant = null
            }).ToList();
        }

        public async Task<List<TenantPositionDto>> GetPositionsByFloorWithTenantAsync(int floor)
        {
            var list = await _db.TblTenantPositions
                .AsNoTracking()
                // CHÚ Ý: đổi tên navigation property nếu model EF của bạn đặt tên khác
                .Include(p => p.TenantPositionAssignedTenant)
                .Where(p => p.TenantPositionFloor == floor)
                .OrderBy(p => p.TenantPositionId)
                .ToListAsync();

            if (list == null || !list.Any()) return new List<TenantPositionDto>();

            return list.Select(p => new TenantPositionDto
            {
                TenantPosition_ID = p.TenantPositionId,
                TenantPosition_Location = p.TenantPositionLocation,
                TenantPosition_AssignedTenantID = p.TenantPositionAssignedTenantId,
                TenantPosition_Area_M2 = p.TenantPositionAreaM2,
                TenantPosition_Floor = p.TenantPositionFloor,
                TenantPosition_Status = p.TenantPositionStatus.HasValue ? (int)p.TenantPositionStatus : 0,
                TenantPosition_LeftPct = p.TenantPositionLeftPct,
                TenantPosition_TopPct = p.TenantPositionTopPct,
                Tenant = p.TenantPositionAssignedTenant == null ? null : new TenantDto
                {
                    Tenant_Id = p.TenantPositionAssignedTenant.TenantId,
                    Tenant_Name = p.TenantPositionAssignedTenant.TenantName,
                    Tenant_Img = p.TenantPositionAssignedTenant.TenantImg,
                    Tenant_UserID = p.TenantPositionAssignedTenant.TenantUserId.ToString() ?? "",
                    Tenant_Status = p.TenantPositionAssignedTenant.TenantStatus
                }
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

            // mapping basic pos
            var dto = new TenantPositionDto
            {
                TenantPosition_ID = p.TenantPositionId,
                TenantPosition_Location = p.TenantPositionLocation,
                TenantPosition_AssignedTenantID = p.TenantPositionAssignedTenantId,
                TenantPosition_Area_M2 = p.TenantPositionAreaM2,
                TenantPosition_Floor = p.TenantPositionFloor,
                TenantPosition_Status = p.TenantPositionStatus.HasValue ? (int)p.TenantPositionStatus : 0,
                TenantPosition_LeftPct = p.TenantPositionLeftPct,
                TenantPosition_TopPct = p.TenantPositionTopPct,
                Tenant = null
            };

            // nếu có assigned tenant id -> load tenant basic info
            if (p.TenantPositionAssignedTenantId.HasValue)
            {
                var tid = p.TenantPositionAssignedTenantId.Value;

                // CHÚ Ý: thay _db.TblTenants, TenantId, TenantName... bằng tên DbSet / property thực tế của bạn
                var t = await _db.TblTenants.FirstOrDefaultAsync(x => x.TenantId == tid);
                if (t != null)
                {
                    dto.Tenant = new TenantDto
                    {
                        Tenant_Id = t.TenantId,
                        Tenant_Name = t.TenantName,
                        Tenant_Img = t.TenantImg,
                        Tenant_UserID = t.TenantUserId.ToString(),
                        Tenant_Status = t.TenantStatus
                    };
                }
            }

            return dto;
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
                .Include(p => p.TenantPositionAssignedTenant)
                .Include(p => p.TenantPositionAssignedCinema)
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
                    (p.TenantPositionAssignedTenant != null && p.TenantPositionAssignedTenant.TenantName.ToLower().Contains(search)) ||
                    (p.TenantPositionAssignedCinema != null && p.TenantPositionAssignedCinema.CinemaName.ToLower().Contains(search))
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
                .ThenInclude(t => t.TenantUser)
                .Include(p => p.TenantPositionAssignedCinema)
                .FirstOrDefaultAsync(p => p.TenantPositionId == id);
        }

        // ==========================================================
        // === PARKING METHODS (7-level car park) ===
        // ==========================================================

        /// <summary>
        /// Load toàn bộ level bãi đậu xe + danh sách chỗ đậu (Tbl_ParkingLevel + Tbl_ParkingSpot).
        /// </summary>
        public async Task<List<ParkingLevelDto>> GetParkingLevelsWithSpotsAsync()
        {
            // DbSet tên theo chuẩn scaffold từ Tbl_ParkingLevel / Tbl_ParkingSpot
            var levels = await _db.TblParkingLevels
                .AsNoTracking()
                .OrderBy(l => l.LevelId)
                .ToListAsync();

            if (!levels.Any()) return new List<ParkingLevelDto>();

            var levelIds = levels.Select(l => l.LevelId).ToList();

            var spots = await _db.TblParkingSpots
                .AsNoTracking()
                .Where(s => levelIds.Contains(s.SpotLevelId))
                .OrderBy(s => s.SpotRow)
                .ThenBy(s => s.SpotCol)
                .ToListAsync();

            var result = new List<ParkingLevelDto>();

            foreach (var lvl in levels)
            {
                var dto = new ParkingLevelDto
                {
                    LevelId = lvl.LevelId,
                    LevelName = lvl.LevelName,
                    LevelCapacity = lvl.LevelCapacity
                };

                dto.Spots = spots
                    .Where(s => s.SpotLevelId == lvl.LevelId)
                    .Select(s => new ParkingSpotDto
                    {
                        ParkingSpotId = s.ParkingSpotId,
                        SpotLevelId = s.SpotLevelId,
                        SpotCode = s.SpotCode,
                        SpotRow = s.SpotRow,
                        SpotCol = s.SpotCol,
                        SpotStatus = s.SpotStatus
                    })
                    .ToList();

                result.Add(dto);
            }

            return result;
        }

        // ==========================================================
        // === LEASE / CONTRACT HELPERS ===
        // ==========================================================

        public async Task<List<TblTenantPosition>> GetExpiringLeasesWithContactAsync(int daysThreshold = 30)
        {
            var today = DateTime.Now;
            var thresholdDate = today.AddDays(daysThreshold);

            return await _db.TblTenantPositions
                .Include(p => p.TenantPositionAssignedTenant)
                    .ThenInclude(t => t.TenantUser) // Quan trọng: Include TenantUser để lấy Email
                .Where(p => p.PositionLeaseEnd.HasValue &&
                            p.PositionLeaseEnd.Value > today &&
                            p.PositionLeaseEnd.Value <= thresholdDate)
                .ToListAsync();
        }

        //Partner Method
        public async Task<List<Position>> GetAllPositionsByTenantId(int tenantId)
        {
            return await _db.TblTenantPositions
                .Where(x => x.TenantPositionAssignedTenantId == tenantId)
                .Select(x => new Position
                {
                    Id = x.TenantPositionId,
                    Location = x.TenantPositionLocation,
                    Floor = x.TenantPositionFloor,
                    Area = x.TenantPositionAreaM2,
                    PricePerM2 = x.TenantPositionRentPricePerM2,
                    Status = x.TenantPositionStatus ?? 0,
                    AssignedTenantId = x.TenantPositionAssignedTenantId ?? 0,
                    Start = x.PositionLeaseStart ?? DateTime.MinValue,
                    End = x.PositionLeaseEnd ?? DateTime.MinValue
                })
                .ToListAsync();
        }
        public async Task<Position?> FindById(int id)
        {
            return await _db.TblTenantPositions
                .Where(t => t.TenantPositionId == id)
                .Select(t => new Position
                {
                    Id = t.TenantPositionId,
                    Location = t.TenantPositionLocation,
                    Floor = t.TenantPositionFloor,
                    Area = t.TenantPositionAreaM2,
                    PricePerM2 = t.TenantPositionRentPricePerM2,
                    Status = t.TenantPositionStatus ?? 0,
                    AssignedTenantId = t.TenantPositionAssignedTenantId ?? 0,
                    Start = t.PositionLeaseStart ?? DateTime.MinValue,
                    End = t.PositionLeaseEnd ?? DateTime.MinValue
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> CheckProductNameAsync(string name, int cateId, int? excludeId = null)
        {
            string normalizedInput = NormalizeName(name);

            var allProductNames = await _db.TblProducts
                .Where(t => t.ProductCategoryId == cateId &&
                (!excludeId.HasValue || t.ProductId != excludeId.Value))
                .Select(t => t.ProductName)
                .ToListAsync();

            return allProductNames.Any(dbName => NormalizeName(dbName) == normalizedInput);
        }
        private string NormalizeName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Bỏ khoảng trắng và chuyển về chữ thường
            return new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
        }

        public string NormalizeSearch(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string lower = input.ToLowerInvariant();
            string normalized = lower.Normalize(NormalizationForm.FormD);

            StringBuilder sb = new StringBuilder();
            foreach (char c in normalized)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return new string(sb.ToString()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());
        }
    }
}
