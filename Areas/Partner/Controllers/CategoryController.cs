using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Admin.Models;
using Semester03.Areas.Partner.Models;
using Semester03.Models.Repositories;
using System.Security.Claims;

namespace Semester03.Areas.Partner.Controllers
{
    [Area("Partner")]
    public class CategoryController : Controller
    {
        private readonly CategoryRepository _categoryRepo;
        private readonly TenantRepository _tenantRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;
        // Inject repository qua constructor
        public CategoryController(IWebHostEnvironment webHostEnvironment, CategoryRepository categoryRepo, TenantRepository tenantRepo)
        {
            _webHostEnvironment = webHostEnvironment;
            _categoryRepo = categoryRepo;
            _tenantRepo = tenantRepo;
        }

        public async Task<IActionResult> Index(int id, int page = 1, string search = "")
        {
            const int pageSize = 10;

            // Lấy toàn bộ tenant types từ repository (async)
            var list = await _categoryRepo.GetAllCategoriesByTenantId(id);

            string normalizedSearch = _categoryRepo.NormalizeSearch(search);
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                list = list
                    .Where(t => _categoryRepo.NormalizeSearch(t.Name).Contains(normalizedSearch))
                    .ToList();
            }

            var totalItems = list.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var data = list
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var itemTenant = await _tenantRepo.FindById(id);
            ViewBag.itemTenant = itemTenant;
            ViewBag.listCategory = data;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.StartIndex = (page - 1) * pageSize + 1;
            ViewBag.Search = search ?? "";

            return View();
        }
        [HttpPost]
        public async Task<ActionResult> AddCategory(IFormFile upFile)
        {
            string? categoryName = Request.Form["CategoryName"];
            string? tenant = Request.Form["TenantId"];
            int tenantId = int.Parse(tenant);
            string? categoryStatus = Request.Form["CategoryStatus"];
            int status = Convert.ToInt32(categoryStatus); 
            string fileName = "";
            string pathSave = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            Directory.CreateDirectory(pathSave);
            try
            {
                if (upFile != null && upFile.Length > 0)
                {
                    fileName = $"{DateTime.Now.Ticks}_{Path.GetFileName(upFile.FileName)}";
                    string filePath = Path.Combine(pathSave, fileName);


                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        upFile.CopyTo(stream);
                    }
                }
                else
                {
                    fileName = "noimage.png";
                }
                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    TempData["ErrorMessage"] = "Please enter enough information!";
                    return RedirectToAction("Index", "Category");
                }

            
                var entity = new Category
                {
                    Name = categoryName,
                    Image = fileName,
                    TenantId = tenantId,
                    Status = status
                };

                await _categoryRepo.AddCategory(entity);

                TempData["SuccessMessage"] = "Add tenant success!";
            }
            catch (Exception)
            {

                throw;
            }
            return RedirectToAction("Index", "Category", new {id = tenantId});
        }

        public async Task<ActionResult> Edit(int id)
        {
            var itemCate = await _categoryRepo.FindById(id);
            ViewBag.itemCategory = itemCate;
            return View();
        }
        [HttpGet]
        public async Task<ActionResult> DeleteCate(int id, int tenantId)
        {
            bool res = await _categoryRepo.DeleteCategory(id);
            if (res)
            {
                TempData["SuccessMessage"] = "Delete category success";
            }
            else
            {
                TempData["ErrorMessage"] = "Please delete products in this category";
            }
            return RedirectToAction("Index", "Category", new { id = tenantId });
        }

        public async Task<ActionResult> UpdateCategory(IFormFile upFile)
        {
            string? cate = Request.Form["CategoryId"];
            string? tenant = Request.Form["TenantId"];
            string? cateName = Request.Form["CategoryName"];
            string? cateStatus = Request.Form["CategoryStatus"];
            int status = Convert.ToInt32(cateStatus);
            int tenantId = Convert.ToInt32(tenant);
            int cateId = Convert.ToInt32(cate);
            string fileName = "";
            string pathSave = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");

            Directory.CreateDirectory(pathSave);

            try
            {
                if (upFile != null && upFile.Length > 0)
                {
                    fileName = $"{DateTime.Now.Ticks}_{Path.GetFileName(upFile.FileName)}";
                    string filePath = Path.Combine(pathSave, fileName);


                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        upFile.CopyTo(stream);
                    }
                }
                else
                {
                    fileName = Request.Form["OldImage"];
                }
                bool exists = await _categoryRepo.CheckCategoryNameAsync(cateName, cateId);
                if (exists)
                {
                    TempData["ErrorMessage"] = "Category name already exist";
                    return RedirectToAction("Index", "Category", new { id = tenantId });
                }
                var model = new Category
                {
                    Id = cateId,
                    Name = cateName,
                    Image = fileName,
                    Status = status,
                    TenantId = tenantId,
                };

                bool result = await _categoryRepo.UpdateCategory(model);
                if (!result)
                {
                    TempData["ErrorMessage"] = "Update failed, category not found!";
                }
                TempData["SuccessMessage"] = "Update category success!";
            }
            catch (Exception ex)
            {
                var error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "Error: " + error;
            }

            return RedirectToAction("Index", "Category", new { id = tenantId });
        }
    }
}
