using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Repositories;
using Semester03.Models.ViewModels;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class StoresController : Controller
    {
        private readonly TenantRepository _tenantRepo;

        public StoresController(TenantRepository tenantRepo)
        {
            _tenantRepo = tenantRepo;
        }

        public IActionResult Index(int? typeId, string search)
        {
            var stores = _tenantRepo.GetStores(typeId, search);
            var tenantTypes = TenantTypeRepository.Instance.GetAll();

            string currentTypeName = "Stores";
            if (typeId.HasValue)
            {
                var t = tenantTypes.Find(tt => tt.Id == typeId.Value);
                if (t != null) currentTypeName = t.Name;
            }

            ViewBag.CurrentTypeName = currentTypeName;
            ViewBag.TenantTypes = tenantTypes;
            ViewBag.SearchQuery = search ?? "";

            return View(stores);
        }
    }
}
