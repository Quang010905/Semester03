using Microsoft.AspNetCore.Mvc;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class RoleController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
