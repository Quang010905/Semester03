using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Repositories;
using System.Security.Claims;

namespace Semester03.Areas.Partner.Controllers
{
    [Area("Partner")]
    public class ShopController : Controller
    {
        private readonly UserRepository _userRepo;
        private readonly TenantRepository _tenantRepo;
        private readonly TenantTypeRepository _tenantTypeRepo;
        public ShopController(UserRepository userRepo,  TenantRepository tenantRepo, TenantTypeRepository tenantTypeRepo)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _tenantRepo = tenantRepo ?? throw new ArgumentException(nameof(_tenantRepo));
            _tenantTypeRepo = tenantTypeRepo ?? throw new ArgumentNullException(nameof(_tenantTypeRepo));
        }
        public async Task<IActionResult> Index(int page = 1, string search = "")
        {
            const int pageSize = 10;
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "User is not logged in.";
                return RedirectToAction("Login", "Account");
            }

            int userId = int.Parse(userIdString);
            var list = await _tenantRepo.GetTenantByUserId(userId);

            string normalizedSearch = _tenantRepo.NormalizeSearch(search);
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                list = list
                    .Where(t => _tenantRepo.NormalizeSearch(t.Name).Contains(normalizedSearch))
                    .ToList();
            }

            var totalItems = list.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var data = list
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var item = await _userRepo.CreateTenantByUserId(userId);
            var lsTenantType = await _tenantTypeRepo.GetActiveTenantType();
            ViewBag.itemUser = item;
            ViewBag.listTenantType = lsTenantType;
            ViewBag.listTenant = data;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.StartIndex = (page - 1) * pageSize + 1;
            ViewBag.Search = search ?? "";

            return View();
        }
    }
}
