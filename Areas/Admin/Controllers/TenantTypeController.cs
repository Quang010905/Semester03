using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Admin.Models;
using Semester03.Models.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TenantTypeController : Controller
    {
        private readonly TenantTypeRepository _tenantTypeRepo;

        // Inject repository qua constructor
        public TenantTypeController(TenantTypeRepository tenantTypeRepo)
        {
            _tenantTypeRepo = tenantTypeRepo ?? throw new ArgumentNullException(nameof(tenantTypeRepo));
        }

        // Index - async, dùng repository thay vì new DbContext()
        public async Task<IActionResult> Index(int page = 1, string search = "")
        {
            const int pageSize = 10;

            // Lấy toàn bộ tenant types từ repository (async)
            var list = await _tenantTypeRepo.GetAllAsync();

            string normalizedSearch = _tenantTypeRepo.NormalizeSearch(search);
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                list = list
                    .Where(t => _tenantTypeRepo.NormalizeSearch(t.Name).Contains(normalizedSearch))
                    .ToList();
            }

            var totalItems = list.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var data = list
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.listTenantType = data;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.StartIndex = (page - 1) * pageSize + 1;
            ViewBag.Search = search ?? "";

            return View();
        }

        // Edit - sử dụng async FindByIdAsync
        public async Task<ActionResult> Edit(int id)
        {
            var tenantType = await _tenantTypeRepo.FindByIdAsync(id);
            if (tenantType == null)
            {
                return NotFound();
            }

            ViewBag.itemTenantType = tenantType;
            return View();
        }

        // AddTenantType - async
        [HttpPost]
        public async Task<ActionResult> AddTenantType()
        {
            string? name = Request.Form["TenantTypeName"];
            string statusRaw = Request.Form["TenantTypeStatus"];
            int status = (statusRaw == "true" || statusRaw == "1") ? 1 : 0;

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Please enter enough information";
                return RedirectToAction("Index");
            }

            // Kiểm tra trùng tên (repository trả về true nếu có trùng)
            bool exists = await _tenantTypeRepo.CheckTenantTypeNameAsync(name);
            if (exists)
            {
                TempData["ErrorMessage"] = "Tenant type name already exist";
                return RedirectToAction("Index", "TenantType");
            }

            var item = new TenantType
            {
                Name = name,
                Status = status
            };

            await _tenantTypeRepo.AddAsync(item);
            TempData["SuccessMessage"] = "Add tenant type success";
            return RedirectToAction("Index","TenantType");
        }

        // DeleteTenantType - async
        [HttpGet]
        public async Task<ActionResult> DeleteTenantType(int id)
        {
            bool res = await _tenantTypeRepo.DeleteAsync(id);
            if (res)
            {
                TempData["SuccessMessage"] = "Delete tenant type success";
            }
            else
            {
                TempData["ErrorMessage"] = "Please delete tenant in this tenant type";
            }
            return RedirectToAction("Index", "TenantType");
        }

        // UpdateTenantType - async
        [HttpPost]
        public async Task<ActionResult> UpdateTenantType()
        {
            string? tenantTypeIdRaw = Request.Form["TenantTypeId"];
            string? tenantTypeName = Request.Form["TenantTypeName"];
            string statusRaw = Request.Form["TenantTypeStatus"];

            if (!int.TryParse(tenantTypeIdRaw, out int tenantTypeId))
            {
                TempData["ErrorMessage"] = "Invalid tenant type id";
                return RedirectToAction("Index");
            }

            int status = statusRaw.Contains("1") || statusRaw == "true" ? 1 : 0;

            if (string.IsNullOrWhiteSpace(tenantTypeName))
            {
                TempData["ErrorMessage"] = "Please enter enough information";
                return RedirectToAction("Edit", new { id = tenantTypeId });
            }

            // Kiểm tra trùng tên, loại trừ chính id đang update
            bool exists = await _tenantTypeRepo.CheckTenantTypeNameAsync(tenantTypeName, tenantTypeId);
            if (exists)
            {
                TempData["ErrorMessage"] = "Tenant type name already exist";
                return RedirectToAction("Edit", new { id = tenantTypeId });
            }

            var item = new TenantType
            {
                Id = tenantTypeId,
                Name = tenantTypeName,
                Status = status
            };

            bool updated = await _tenantTypeRepo.UpdateAsync(item);
            if (updated)
            {
                TempData["SuccessMessage"] = "Update Success";
            }
            else
            {
                TempData["ErrorMessage"] = "Update failed";
            }

            return RedirectToAction("Index", "TenantType");
        }
    }
}
