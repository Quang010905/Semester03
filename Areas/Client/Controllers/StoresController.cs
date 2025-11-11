using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Repositories;
using Semester03.Models.ViewModels;
using System.Collections.Generic;
using System.Security.Claims;

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

        // Trang danh sách stores
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

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            bool success = _tenantRepo.AddTenantComment(tenantId, userId, rate, text);

            return Json(new
            {
                success,
                message = success ? "Bình luận đã gửi, chờ duyệt." : "Có lỗi xảy ra."
            });
        }
    }
}
