using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Admin.Models;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TenantTypeController : Controller
    {
        public IActionResult Index(int page = 1, string search = "")
        {
            int pageSize = 2;
            var db = new AbcdmallContext();
            var list = db.TblTenantTypes.ToList();
            string normallizedSearch = TenantTypeRepository.Instance.NormalizeSearch(search);
            if (!string.IsNullOrWhiteSpace(normallizedSearch))
            {
                list = list.Where(t => TenantTypeRepository.Instance.NormalizeSearch(t.TenantTypeName).Contains(normallizedSearch)).ToList();
            }
            var totalItems = list.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var data = list
                .OrderBy(c => c.TenantTypeName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new TenantType
                {
                    Id = c.TenantTypeId,
                    Name = c.TenantTypeName,
                    Status = c.TenantTypeStatus ?? 0,
                }).ToList();
            ViewBag.listTenantType = data;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.StartIndex = (page - 1) * pageSize + 1;
            ViewBag.Search = search;
            return View();
        }
        public ActionResult Edit(int id)
        {
            var tenantType = TenantTypeRepository.Instance.FindById(id);
            ViewBag.itemTenantType = tenantType;
            return View();
        }
        public  ActionResult AddTenantType()
        {
            string? name = Request.Form["TenantTypeName"];
            int status = Request.Form["TenantTypeStatus"] == "true" ? 1 : 0;
            string normalizedName = TenantTypeRepository.Instance.NormalizeName(name);
            if (string.IsNullOrEmpty(name))
            {
                TempData["ErrorMessage"] = "Please enter enough information";
                return RedirectToAction("Index");
            }
            if (TenantTypeRepository.Instance.checkTenantTypeName(normalizedName))
            {
                TempData["ErrorMessage"] = "Tenant type name already exist";
                return RedirectToAction("Index", "TenantType");
            }
            var item = new TenantType
            {
                Name = name,
                Status = status
            };
            TenantTypeRepository.Instance.Add(item);
            TempData["SuccessMessage"] = "Add tenant type success";
            return RedirectToAction("Index");
        }
        public ActionResult DeleteTenantType(int id) 
        {
            bool res = TenantTypeRepository.Instance.Delete(id);
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
        public ActionResult UpdateTenantType()
        {
            string? tenantTypeId = Request.Form["TenantTypeId"];
            string? tenantTypeName = Request.Form["TenantTypeName"];
            int status = Request.Form["TenantTypeStatus"].Contains("1") ? 1 : 0;
            string normalizedName = TenantTypeRepository.Instance.NormalizeName(tenantTypeName).ToLower();
            if (string.IsNullOrEmpty(tenantTypeName))
            {
                TempData["ErrorMessage"] = "Please enter enough information";
                return RedirectToAction("Edit", new { Id = int.Parse(tenantTypeId) });
            }
            if (TenantTypeRepository.Instance.checkTenantTypeName(normalizedName, int.Parse(tenantTypeId)))
            {
                TempData["ErrorMessage"] = "Tenant type name already exist";
                return RedirectToAction("Edit", new { Id = int.Parse(tenantTypeId) });
            }
            var item = new TenantType
            {
                Id = int.Parse(tenantTypeId),
                Name = tenantTypeName,
                Status = status
            };
            TenantTypeRepository.Instance.Update(item);
            TempData["SuccessMessage"] = "Update Success";
            return RedirectToAction("Edit", new { Id = int.Parse(tenantTypeId) });
        }
    }
}
