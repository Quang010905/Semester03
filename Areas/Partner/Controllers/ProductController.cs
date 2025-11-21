using Microsoft.AspNetCore.Mvc;

namespace Semester03.Areas.Partner.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
