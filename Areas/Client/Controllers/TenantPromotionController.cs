using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using Semester03.Models.ViewModels;


namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class TenantPromotionController : Controller
    {
        private readonly AbcdmallContext _context;

        public TenantPromotionController(AbcdmallContext context)
        {
            _context = context;
        }

        // LIST PAGE
        public IActionResult Index(int? tenantId, int page = 1)
        {
            int pageSize = 8;

            var query = _context.TblTenantPromotions
                .Include(x => x.TenantPromotionTenant)
                .OrderByDescending(x => x.TenantPromotionStart)
                .AsQueryable();

            if (tenantId.HasValue)
                query = query.Where(x => x.TenantPromotionTenantId == tenantId);

            var today = DateTime.Now;

            var activeList = query
                .Where(p => p.TenantPromotionEnd >= today)
                .Select(p => new TenantPromotionVm
                {
                    Id = p.TenantPromotionId,
                    Title = p.TenantPromotionTitle,
                    Img = p.TenantPromotionImg,
                    PromotionStart = p.TenantPromotionStart,
                    PromotionEnd = p.TenantPromotionEnd,
                    DiscountAmount = p.TenantPromotionDiscountAmount,
                    DiscountPercent = p.TenantPromotionDiscountPercent
                });

            int totalItems = activeList.Count();
            var items = activeList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var expired = query
                .Where(p => p.TenantPromotionEnd < today)
                .OrderByDescending(p => p.TenantPromotionEnd)
                .Select(p => new TenantPromotionVm
                {
                    Id = p.TenantPromotionId,
                    Title = p.TenantPromotionTitle,
                    Img = p.TenantPromotionImg,
                    PromotionEnd = p.TenantPromotionEnd,
                }).ToList();

            var vm = new TenantPromotionListVm
            {
                SelectedTenantId = tenantId,
                Tenants = _context.TblTenants.ToList(),
                ActivePromotions = items,
                ExpiredPromotions = expired,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return View(vm);
        }



        // DETAIL PAGE
        public IActionResult Detail(int id)
        {
            var p = _context.TblTenantPromotions
                .Include(x => x.TenantPromotionTenant)
                .FirstOrDefault(x => x.TenantPromotionId == id);

            if (p == null) return NotFound();

            var vm = new TenantPromotionVm
            {
                Id = p.TenantPromotionId,
                Title = p.TenantPromotionTitle,
                Img = p.TenantPromotionImg,
                Description = p.TenantPromotionDescription,
                PromotionStart = p.TenantPromotionStart,
                PromotionEnd = p.TenantPromotionEnd,
                DiscountAmount = p.TenantPromotionDiscountAmount,
                DiscountPercent = p.TenantPromotionDiscountPercent,
                MinBillAmount = p.TenantPromotionMinBillAmount
            };

            return View(vm);
        }
    }
}
