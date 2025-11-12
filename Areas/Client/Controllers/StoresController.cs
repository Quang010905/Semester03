using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Repositories;
using Semester03.Models.ViewModels;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class StoresController : Controller
    {
        private readonly TenantRepository _tenantRepo;
        private readonly TenantTypeRepository _tenantTypeRepo;

        // Inject cả TenantRepository và TenantTypeRepository qua DI
        public StoresController(TenantRepository tenantRepo, TenantTypeRepository tenantTypeRepo)
        {
            _tenantRepo = tenantRepo;
            _tenantTypeRepo = tenantTypeRepo;
        }

        // Trang danh sách stores (async để await tenantTypeRepo)
        public async Task<IActionResult> Index(int? typeId, string search)
        {
            // Lấy stores (giữ nguyên phương thức hiện tại của bạn)
            var stores = _tenantRepo.GetStores(typeId, search);

            // Lấy tenant types bằng repository (async)
            var tenantTypes = await _tenantTypeRepo.GetAllAsync();

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

        // Trang chi tiết store
        [HttpGet]
        public IActionResult Details(int id)
        {
            var tenant = _tenantRepo.GetTenantDetails(id);
            if (tenant == null) return NotFound();
            return View(tenant);
        }

        // Thêm bình luận tenant (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddComment(int tenantId, int rate, string text)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized(new { success = false, message = "Bạn cần đăng nhập." });

            // lấy userId từ claim
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return BadRequest(new { success = false, message = "Không xác định được user." });
            }

            bool success = _tenantRepo.AddTenantComment(tenantId, userId, rate, text);

            return Json(new
            {
                success,
                message = success ? "Bình luận đã gửi, chờ duyệt." : "Có lỗi xảy ra."
            });
        }
    }
}
