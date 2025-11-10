using Semester03.Models.Entities;
using Semester03.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Semester03.Models.Repositories
{
    public class TenantRepository
    {
        private readonly AbcdmallContext _context;

        public TenantRepository(AbcdmallContext context)
        {
            _context = context;
        }

        // Lấy Featured Stores (top 6)
        public List<FeaturedStoreViewModel> GetFeaturedStores(int top = 6)
        {
            return _context.TblTenants
                .Include(t => t.TenantType)
                .Include(t => t.TblTenantPositions)
                .OrderByDescending(t => t.TenantCreatedAt)
                .Take(top)
                .Select(t => new FeaturedStoreViewModel
                {
                    TenantId = t.TenantId,
                    TenantName = t.TenantName,
                    TenantImg = t.TenantImg,
                    TenantDescription = t.TenantDescription ?? "",
                    TenantTypeName = t.TenantType.TenantTypeName,
                    Position = t.TblTenantPositions.FirstOrDefault() != null
                        ? $"{t.TblTenantPositions.First().TenantPositionLocation}, Floor {t.TblTenantPositions.First().TenantPositionFloor}"
                        : ""
                })
                .ToList();
        }

        // Lấy stores theo typeId và search
        public List<FeaturedStoreViewModel> GetStores(int? typeId = null, string search = "")
        {
            var query = _context.TblTenants
                .Include(t => t.TenantType)
                .Include(t => t.TblTenantPositions)
                .AsQueryable();

            if (typeId.HasValue)
                query = query.Where(t => t.TenantTypeId == typeId.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => t.TenantName.Contains(search));

            return query.OrderBy(t => t.TenantName)
                        .Select(t => new FeaturedStoreViewModel
                        {
                            TenantId = t.TenantId,
                            TenantName = t.TenantName,
                            TenantImg = t.TenantImg,
                            TenantTypeName = t.TenantType.TenantTypeName,
                            TenantDescription = t.TenantDescription ?? "",
                            Position = t.TblTenantPositions.FirstOrDefault() != null
                                ? $"{t.TblTenantPositions.First().TenantPositionLocation}, Floor {t.TblTenantPositions.First().TenantPositionFloor}"
                                : ""
                        })
                        .ToList();
        }
    }
}
