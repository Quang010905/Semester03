using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Repositories;
using Semester03.Models.ViewModels;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class TenantPromotionController : ClientBaseController
    {
        private readonly TenantPromotionRepository _promotionRepo;

        public TenantPromotionController(
            TenantTypeRepository tenantTypeRepo,
            TenantPromotionRepository promotionRepo
        ) : base(tenantTypeRepo)
        {
            _promotionRepo = promotionRepo;
        }

        // LIST PAGE
        public IActionResult Index(int? tenantId, int page = 1)
        {
            // Chỉ gọi repository, tất cả logic query + phân trang đã ở repo
            var vm = _promotionRepo.GetPromotions(tenantId, page);
            return View(vm);
        }

        // DETAIL PAGE
        public IActionResult Detail(int id)
        {
            var vm = _promotionRepo.GetPromotionDetail(id);
            if (vm == null) return NotFound();
            return View(vm);
        }
    }
}
