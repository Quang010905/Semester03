using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Admin.Models;
using Semester03.Models.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TenantController : Controller
    {
        private readonly UserRepository _userRepo;
        private readonly TenantTypeRepository _tenantTypeRepo;
        private readonly TenantRepository _tenantRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;
        // Inject repository qua constructor
        public TenantController(UserRepository userRepo, TenantTypeRepository tenantTypeRepo, TenantRepository tenantRepo,IWebHostEnvironment webHostEnvironment)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _tenantTypeRepo = tenantTypeRepo ?? throw new ArgumentNullException(nameof(_tenantTypeRepo));
            _tenantRepo = tenantRepo ?? throw new ArgumentException(nameof(_tenantRepo));
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(int page = 1, string search = "")
        {
            const int pageSize = 10;

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

        public async Task<ActionResult> CreateTenant(int id, int page = 1, string search = "")
        {
            int pageSize = 10;
            var listTenantByUserId = await _tenantRepo.GetTenantByUserId(id);
            string normalizedSearch = _userRepo.NormalizeSearch(search);
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                listTenantByUserId = listTenantByUserId
                    .Where(t => _userRepo.NormalizeSearch(t.Name).Contains(normalizedSearch))
                    .ToList();
            }
            var totalItems = listTenantByUserId.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var data = listTenantByUserId
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var item  = await _userRepo.CreateTenantByUserId(id);
            var lsTenantType = await _tenantTypeRepo.GetActiveTenantType();
            var lsTenant = await _tenantRepo.GetTenantByUserId(id);
            if (item == null)
            {
                return NotFound();
            }
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
        [HttpPost]
        public async Task<ActionResult> AddTenant(IFormFile upFile)
        {
            string? tenantName = Request.Form["TenantName"];
            string? typeId = Request.Form["TenantTypeId"];
            string? userId = Request.Form["UserId"];
            string? description = Request.Form["Description"];
            string? tenantStatus = Request.Form["TenantStatus"];
            int status = Convert.ToInt32(tenantStatus); // null => 0
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
                if (string.IsNullOrWhiteSpace(description))
                {
                    TempData["ErrorMessage"] = "Please enter enough information!";
                    return RedirectToAction("CreateTenant", new { id = userId });
                }

                if (string.IsNullOrWhiteSpace(typeId) || typeId == "0")
                {
                    TempData["ErrorMessage"] = "Please enter enough information!";
                    return RedirectToAction("CreateTenant", new { id = userId });
                }
                bool exists = await _tenantRepo.CheckTenantNameAsync(tenantName);
                if (exists)
                {
                    TempData["ErrorMessage"] = "Tenant name already exist";
                    return RedirectToAction("CreateTenant", "Tenant", new { id = userId });
                }
                var entity = new Tenant
                {
                    Name = tenantName,
                    TypeId = int.Parse(typeId),
                    UserId = int.Parse(userId),
                    Image = fileName,
                    Description = description,
                    Status = status
                };

                await _tenantRepo.AddAsync(entity);

                TempData["SuccessMessage"] = "Add tenant success!";
            }
            catch (Exception)
            {

                throw;
            }
            return RedirectToAction("CreateTenant", "Tenant", new { id =  userId});
        }
        public async Task<ActionResult> Edit(int id)
        {
            var itemTenant = await _tenantRepo.FindById(id);
            var lsTenantType = await _tenantTypeRepo.GetActiveTenantType();
            ViewBag.itemTenant = itemTenant;
            ViewBag.listTenantType = lsTenantType;
            return View();
        }

        [HttpGet]
        public async Task<ActionResult> DeleteTenant(int id, int userId)
        {
            bool res = await _tenantRepo.Delete(id);
            if (res)
            {
                TempData["SuccessMessage"] = "Delete tenant success";
            }
            else
            {
                TempData["ErrorMessage"] = "Please delete categories in this tenant";
            }
            return RedirectToAction("CreateTenant", new { id =  userId});
        }

        public async Task<ActionResult> UpdateTenant(IFormFile upFile)
        {
            string? Id = Request.Form["TenantId"];
            string? tenantName = Request.Form["TenantName"];
            string? typeId = Request.Form["TenantTypeId"];
            string? userId = Request.Form["UserId"];
            string? description = Request.Form["Description"];
            string? tenantStatus = Request.Form["TenantStatus"];
            int status = Convert.ToInt32(tenantStatus);
            int tenantId = Convert.ToInt32(Id);
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
                if (string.IsNullOrWhiteSpace(description))
                {
                    TempData["ErrorMessage"] = "Please enter enough information!";
                    return RedirectToAction("CreateTenant", new { id = userId });
                }

                if (string.IsNullOrWhiteSpace(typeId) || typeId == "0")
                {
                    TempData["ErrorMessage"] = "Please enter enough information!";
                    return RedirectToAction("CreateTenant", new { id = userId });
                }

                bool exists = await _tenantRepo.CheckTenantNameAsync(tenantName, tenantId);
                if (exists)
                {
                    TempData["ErrorMessage"] = "Tenant name already exist";
                    return RedirectToAction("CreateTenant", "Tenant", new { id = userId });
                }
                var model = new Tenant
                {
                    Id = tenantId,
                    Name = tenantName,
                    TypeId = int.Parse(typeId),
                    UserId = int.Parse(userId),
                    Image = fileName,
                    Description = description,
                    Status = status
                };

                bool result = await _tenantRepo.Update(model);
                if (!result)
                {
                    TempData["ErrorMessage"] = "Update failed, tenant not found!";
                }
                TempData["SuccessMessage"] = "Update tenant success!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }

            return RedirectToAction("CreateTenant", "Tenant", new { id = userId });
        }
    }
}
