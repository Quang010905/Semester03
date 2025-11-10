using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class FeaturedStoresController : Controller
    {
        private readonly TenantRepository _tenantRepo;

        public FeaturedStoresController(TenantRepository tenantRepo)
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
