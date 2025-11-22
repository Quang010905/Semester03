using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class FeaturedStoresController : ClientBaseController
    {
        private readonly TenantRepository _tenantRepo;

        public FeaturedStoresController(
            TenantTypeRepository tenantTypeRepo,    // thêm dòng này
            TenantRepository tenantRepo
        ) : base(tenantTypeRepo)                   // gọi base
        {
            _tenantRepo = tenantRepo;
        }

        public IActionResult Index()
        {
            var stores = _tenantRepo.GetFeaturedStores();
            return PartialView("_StoreListPartial", stores);
        }
    }
}
