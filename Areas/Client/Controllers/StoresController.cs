using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using Semester03.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class StoresController : Controller
    {
        private readonly TenantRepository _tenantRepo;
        private readonly TenantTypeRepository _tenantTypeRepo;
        private readonly AbcdmallContext _context;

        // Inject TenantRepository, TenantTypeRepository and DbContext
        public StoresController(
            TenantRepository tenantRepo,
            TenantTypeRepository tenantTypeRepo,
            AbcdmallContext context)
        {
            _tenantRepo = tenantRepo;
            _tenantTypeRepo = tenantTypeRepo;
            _context = context;
        }

        // =======================
        // 1️⃣ Trang danh sách stores
        // =======================
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

        // =======================
        // 2️⃣ Trang chi tiết store
        // =======================
        [HttpGet]
        public IActionResult Details(int id)
        {
            var model = _tenantRepo.GetTenantDetails(id);
            if (model == null) return NotFound();

            // Gán luôn danh mục sản phẩm (nếu repo có phương thức này)
            // Nếu phương thức trả về null hoặc không tồn tại, bạn có thể thay bằng truy vấn trực tiếp qua _context.
            model.ProductCategories = _tenantRepo.GetProductCategoriesByTenant(id) ?? new List<ProductCategoryVm>();

            return View(model);
        }

        // =======================
        // 3️⃣ Thêm bình luận tenant (AJAX)
        // =======================
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

        // =======================
        // 4️⃣ Lấy sản phẩm theo danh mục (AJAX)
        // =======================
        [HttpGet]
        public IActionResult GetProductsByCategory(int categoryId)
        {
            var products = _context.TblProducts
                .Where(p => p.ProductCategoryId == categoryId && (p.ProductStatus == 1 || p.ProductStatus == null))
                .Select(p => new ProductVm
                {
                    Id = p.ProductId,
                    Name = p.ProductName,
                    Img = p.ProductImg,
                    Price = p.ProductPrice
                })
                .ToList();

            return Json(products);
        }
    }
}
