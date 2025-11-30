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
    public class StoresController : ClientBaseController
    {
        private readonly TenantRepository _tenantRepo;
        private readonly AbcdmallContext _context;

        public StoresController(
            TenantRepository tenantRepo,
            TenantTypeRepository tenantTypeRepo,
            AbcdmallContext context
        ) : base(tenantTypeRepo)
        {
            _tenantRepo = tenantRepo;
            _context = context;
        }

        // =======================
        // 1️⃣ Store listing page
        // =======================
        [HttpGet]
        public async Task<IActionResult> Index(int? typeId, string search, int page = 1)
        {
            int pageSize = 18;

            var storesQuery = _tenantRepo.GetStores(typeId, search).AsQueryable();

            int totalItems = storesQuery.Count();

            var stores = storesQuery
                .OrderBy(s => s.TenantName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var tenantTypes = await base._tenantTypeRepo.GetAllAsync();
            string currentTypeName = "Stores";

            if (typeId.HasValue)
            {
                var t = tenantTypes.Find(tt => tt.Id == typeId.Value);
                if (t != null) currentTypeName = t.Name;
            }

            ViewBag.CurrentTypeName = currentTypeName;
            ViewBag.TenantTypes = tenantTypes;
            ViewBag.SearchQuery = search ?? "";
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.CurrentTypeId = typeId;

            return View(stores);
        }

        // =======================
        // 2️⃣ Store details page
        // =======================
        [HttpGet]
        public IActionResult Details(int id, int cmtPage = 1)
        {
            int? currentUserId = null;

            if (User?.Identity?.IsAuthenticated == true)
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out var uid))
                {
                    currentUserId = uid;
                }
            }

            const int CommentPageSize = 100; 

            var model = _tenantRepo.GetTenantDetails(id, currentUserId, cmtPage, CommentPageSize);
            if (model == null) return NotFound();

            model.ProductCategories = _tenantRepo.GetProductCategoriesByTenant(id)
                                        ?? new List<ProductCategoryVm>();

            return View(model);
        }


        // =======================
        // 3️⃣ Add tenant comment (AJAX)
        // =======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddComment(int tenantId, int rate, string text)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized(new { success = false, message = "You need to log in." });

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return BadRequest(new { success = false, message = "Cannot determine current user." });
            }

            bool success = _tenantRepo.AddTenantComment(tenantId, userId, rate, text);

            return Json(new
            {
                success,
                message = success
                    ? "Your comment has been submitted and is waiting for approval."
                    : "An error occurred while submitting your comment."
            });
        }

        // =======================
        // 4️⃣ Get products by category (AJAX)
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
