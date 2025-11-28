using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Admin.Models;
using Semester03.Areas.Partner.Models;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Partner.Controllers
{
    [Area("Partner")]
    public class ProductController : Controller
    {
        private readonly CategoryRepository _categoryRepo;
        private readonly ProductRepository _productRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;
        // Inject repository qua constructor
        public ProductController(IWebHostEnvironment webHostEnvironment, CategoryRepository categoryRepo, ProductRepository productRepo)
        {
            _webHostEnvironment = webHostEnvironment;
            _categoryRepo = categoryRepo;
            _productRepo = productRepo;
        }

        public async Task<IActionResult> Index(int id, int page = 1, string search = "")
        {
            const int pageSize = 10;

            // Lấy toàn bộ tenant types từ repository (async)
            var list = await _productRepo.GetAllProductsByCateId(id);

            string normalizedSearch = _productRepo.NormalizeSearch(search);
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                list = list
                    .Where(t => _productRepo.NormalizeSearch(t.Name).Contains(normalizedSearch))
                    .ToList();
            }

            var totalItems = list.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var data = list
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var itemCate = await _categoryRepo.FindById(id);
            ViewBag.itemCate = itemCate;
            ViewBag.listProduct = data;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.StartIndex = (page - 1) * pageSize + 1;
            ViewBag.Search = search ?? "";
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> AddProduct(IFormFile upFile)
        {
            string? productName = Request.Form["ProductName"];
            string? cate = Request.Form["CateId"];
            int cateId = int.Parse(cate);
            string? productStatus = Request.Form["ProductStatus"];
            int status = Convert.ToInt32(productStatus);
            string? pPrice = Request.Form["proPrice"];
            int price = Convert.ToInt32(pPrice);
            string? description = Request.Form["proDescription"];
            bool exists = await _productRepo.CheckProductNameAsync(productName, cateId);
            if (exists)
            {
                TempData["ErrorMessage"] = "Product name already exist";
                return RedirectToAction("Index", "Product", new { id = cateId });
            }
            string fileName = "";
            string pathSave = Path.Combine(_webHostEnvironment.WebRootPath, "Content/Uploads/Product");
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
                if (string.IsNullOrWhiteSpace(productName))
                {
                    TempData["ErrorMessage"] = "Please enter enough information!";
                    return RedirectToAction("Index", "Product", new { id = cateId });
                }
                if (string.IsNullOrWhiteSpace(description))
                {
                    TempData["ErrorMessage"] = "Please enter enough information!";
                    return RedirectToAction("Index", "Product", new { id = cateId });
                }

                var entity = new Product
                {
                    Name = productName,
                    Img = fileName,
                    CateId = cateId,
                    Status = status,
                    Description = description,
                    Price = price,
                    CreatedAt = DateTime.Now
                };

                await _productRepo.AddProduct(entity);

                TempData["SuccessMessage"] = "Add product success!";
            }
            catch (Exception)
            {

                throw;
            }
            return RedirectToAction("Index", "Product", new { id = cateId });
        }

        public async Task<ActionResult> Edit(int id)
        {
            var itemPro = await _productRepo.FindById(id);
            ViewBag.itemPro = itemPro;
            return View();
        }
        [HttpGet]
        public async Task<ActionResult> DeletePro(int id, int cateId)
        {
            bool res = await _productRepo.DeleteProduct(id);
            if (res)
            {
                TempData["SuccessMessage"] = "Delete product success";
            }
            else
            {
                TempData["ErrorMessage"] = "Please delete product fail";
            }
            return RedirectToAction("Index", "Product", new { id = cateId });
        }

        public async Task<ActionResult> UpdatePro(IFormFile upFile)
        {
            string? productId = Request.Form["ProductId"];
            int proId = Convert.ToInt32(productId);
            string? productName = Request.Form["ProductName"];
            string? cate = Request.Form["CateId"];
            int cateId = int.Parse(cate);
            string? productStatus = Request.Form["ProductStatus"];
            int status = productStatus == null ? 0 : 1;
            string? pPrice = Request.Form["ProductPrice"];
            decimal price = Convert.ToDecimal(pPrice);
            string? description = Request.Form["proDescription"];
            string fileName = "";
            string pathSave = Path.Combine(_webHostEnvironment.WebRootPath, "Content/Uploads/Product");

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
                bool exists = await _categoryRepo.CheckCategoryNameAsync(productName, cateId, proId);
                if (exists)
                {
                    TempData["ErrorMessage"] = "Product name already exist";
                    return RedirectToAction("Index", "Product", new { id = cateId });
                }
                var entity = new Product
                {
                    Id = int.Parse(productId),
                    Name = productName,
                    Img = fileName,
                    CateId = cateId,
                    Status = status,
                    Description = description,
                    Price = price,
                    CreatedAt = DateTime.Now
                };

                bool result = await _productRepo.UpdateProduct(entity);
                TempData["SuccessMessage"] = "Update product success!";
            }
            catch (Exception ex)
            {
                var error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "Error: " + error;
            }

            return RedirectToAction("Index", "Product", new { id = cateId });
        }
    }
}
