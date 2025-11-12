using Microsoft.AspNetCore.Mvc;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class HomeController : Controller
    {
        // GET: / hoặc /Home/Index
        public IActionResult Index()
        {
            // Set thông tin hiển thị logo, tên mall, địa chỉ
            ViewData["MallName"] = "ABCD Mall";
            ViewData["MallAddress"] = "123 Main Street";

            return View(); // Trả về view Areas/Client/Views/Home/Index.cshtml
        }

        // POST: /Home/SetLanguage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(culture))
                culture = "en";

            Response.Cookies.Append(
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(
                    new Microsoft.AspNetCore.Localization.RequestCulture(culture)
                ),
                new Microsoft.AspNetCore.Http.CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index");
        }
    }
}
