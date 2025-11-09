using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TenantTypeController : Controller
    {
        public IActionResult Index()
        {
            var lsTenantType = TenantTypeRepository.Instance.GetAll();
            ViewBag.listTenantType = lsTenantType;
            return View();
        }
    }
}
