using Semester03.Models.Entities;
using Semester03.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Semester03.Models.Repositories
{
    public class TenantRepository
    {
        private readonly AbcdmallContext _context;

        public TenantRepository(AbcdmallContext context)
        {
            _context = context;
        }

        // Lấy Featured Stores
        public List<FeaturedStoreViewModel> GetFeaturedStores(int top = 6)
        {
            return _context.TblTenants
                .Include(t => t.TenantType)
                .OrderByDescending(t => t.TenantCreatedAt)
                .Take(top)
                .Select(t => new FeaturedStoreViewModel
                {
                    TenantId = t.TenantId,
                    TenantName = t.TenantName,
                    TenantImg = t.TenantImg,
                    TenantDescription = t.TenantDescription ?? "",
                    TenantTypeName = t.TenantType.TenantTypeName
                }).ToList();
        }
    }
}
