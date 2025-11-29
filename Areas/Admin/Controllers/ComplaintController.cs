using Microsoft.AspNetCore.Mvc;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class ComplaintController : Controller
    {
        public IActionResult Index()    
        {
            return View();
        }
    }
}
