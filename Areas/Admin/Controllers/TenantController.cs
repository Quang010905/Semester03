using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TenantController : Controller
    {
        private readonly UserRepository _userRepo;
        private readonly TenantTypeRepository _tenantTypeRepo;
        // Inject repository qua constructor
        public TenantController(UserRepository userRepo, TenantTypeRepository tenantTypeRepo)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _tenantTypeRepo = tenantTypeRepo ?? throw new ArgumentNullException(nameof(_tenantTypeRepo));
        }

        public async Task<IActionResult> Index(int page = 1, string search = "")
        {
            const int pageSize = 2;

            // Lấy toàn bộ tenant types từ repository (async)
            var list = await _userRepo.GetAllUserFilterByStatus();

            string normalizedSearch = _userRepo.NormalizeSearch(search);
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                list = list
                    .Where(t => _userRepo.NormalizeSearch(t.FullName).Contains(normalizedSearch))
                    .ToList();
            }

            var totalItems = list.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var data = list
                .OrderBy(c => c.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.listUser = data;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.StartIndex = (page - 1) * pageSize + 1;
            ViewBag.Search = search ?? "";

            return View();
        }

        public async Task<ActionResult> CreateTenant(int id)
        {
            var item  = await _userRepo.CreateTenantByUserId(id);
            var lsTenantType = await _tenantTypeRepo.GetActiveTenantType();
            if (item == null)
            {
                return NotFound();
            }
            ViewBag.itemUser = item;
            ViewBag.listTenantType = lsTenantType;
            return View();
        } 
        public async Task<ActionResult> Edit(int id)
        {
            return View();
        }
    }
}
