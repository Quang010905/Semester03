using Microsoft.AspNetCore.Mvc;

namespace Semester03.Areas.Admin.Controllers
{
    public class TenantController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
