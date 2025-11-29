using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Partner.Models;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Partner.Controllers
{
    [Area("Partner")]
    public class TenantPromotionController : Controller
    {
        private readonly TenantPromotionRepository _tenantPromotionRepo;
        private readonly TenantRepository _tenantRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        // Inject repository qua constructor
        public TenantPromotionController(IWebHostEnvironment webHostEnvironment, TenantPromotionRepository tenantPromotionRepo, TenantRepository tenantRepo)
        {
            _webHostEnvironment = webHostEnvironment;
            _tenantPromotionRepo = tenantPromotionRepo;
            _tenantRepository = tenantRepo;
        }

        public async Task<IActionResult> Index(int id, int page = 1, string search = "")
        {
            const int pageSize = 10;

            // Lấy toàn bộ tenant types từ repository (async)
            var list = await _tenantPromotionRepo.GetAllPromotionsByTenantId(id);
            var now = DateTime.Now;
            foreach (var p in list)
            {
                if (p.End < now && p.Status == 1) 
                {
                    p.Status = 0; 
                    await _tenantPromotionRepo.UpdatePromotionStatus(p); 
                }
            }

            string normalizedSearch = _tenantPromotionRepo.NormalizeSearch(search);
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                list = list
                    .Where(t => _tenantPromotionRepo.NormalizeSearch(t.Title).Contains(normalizedSearch))
                    .ToList();
            }
            var itemTenant = await _tenantRepository.FindById(id);
            var totalItems = list.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var data = list
                .OrderByDescending(c => c.Status)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            ViewBag.itemTenant = itemTenant;
            ViewBag.listPromotion = data;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.StartIndex = (page - 1) * pageSize + 1;
            ViewBag.Search = search ?? "";
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> AddPromotion(IFormFile upFile)
        {
            string? title = Request.Form["Title"];
            string? Percent = Request.Form["Percent"];
            string? Amount = Request.Form["Amount"];
            string? MinBill = Request.Form["MinBill"];
            decimal per = string.IsNullOrWhiteSpace(Percent) ? 0 : Convert.ToDecimal(Percent);
            decimal am = string.IsNullOrWhiteSpace(Amount) ? 0 : Convert.ToDecimal(Amount);
            decimal minB = string.IsNullOrWhiteSpace(MinBill) ? 0 : Convert.ToDecimal(MinBill);
            string? TenantId = Request.Form["TenantId"];
            int tenantId = Convert.ToInt32(TenantId);
            string? description = Request.Form["proDescription"];
            string? proStatus = Request.Form["ProStatus"];
            int status = Convert.ToInt32(proStatus);
            string? startStr = Request.Form["Start"];
            string? endStr = Request.Form["End"];

            DateTime startDate = DateTime.Parse(startStr);
            DateTime endDate = DateTime.Parse(endStr);
            //if (startDate.Date < DateTime.Today)
            //{
            //    TempData["ErrorMessage"] = "Start date must be today or in the future!";
            //    return RedirectToAction("Index", "TenantPromotion", new { id = tenantId });
            //}

            if (startDate >= endDate)
            {
                TempData["ErrorMessage"] = "Start date must be before end date!";
                return RedirectToAction("Index", "TenantPromotion", new { id = tenantId });
            }
            if (per > 100)
            {
                TempData["ErrorMessage"] = "Discount percent must be < 100%!";
                return RedirectToAction("Index", "TenantPromotion", new { id = tenantId });
            }
            bool exists = await _tenantPromotionRepo.CheckPromotionAsync(title, tenantId);
            if (exists)
            {
                TempData["ErrorMessage"] = "Title already exist";
                return RedirectToAction("Index", "TenantPromotion", new { id = tenantId });
            }
            string fileName = "";
            string pathSave = Path.Combine(_webHostEnvironment.WebRootPath, "Content/Uploads/TenantPromotion");
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
                if (string.IsNullOrWhiteSpace(title))
                {
                    TempData["ErrorMessage"] = "Please enter enough information!";
                    return RedirectToAction("Index", "TenantPromotion", new { id = tenantId });
                }
                if (string.IsNullOrWhiteSpace(description))
                {
                    TempData["ErrorMessage"] = "Please enter enough information!";
                    return RedirectToAction("Index", "TenantPromotion", new { id = tenantId });
                }

                var entity = new TenantPromotion
                {
                    Title = title,
                    Img = fileName,
                    TenantId = tenantId,
                    Status = status,
                    Description = description,
                    DiscountPercent= per,
                    DiscountAmount = am,
                    MinBillAmount = minB,
                    Start = startDate,
                    End = endDate
                };

                await _tenantPromotionRepo.AddPromotion(entity);

                TempData["SuccessMessage"] = "Add promotion success!";
            }
            catch (Exception)
            {

                throw;
            }
            return RedirectToAction("Index", "TenantPromotion", new { id = tenantId });
        }

        public async Task<ActionResult> Edit(int id)
        {
            var itemProMo = await _tenantPromotionRepo.FindById(id);
            ViewBag.itemProMo = itemProMo;
            return View();
        }
        [HttpGet]
        public async Task<ActionResult> DeletePromotion(int Id, int tenantId)
        {
            bool res = await _tenantPromotionRepo.DeletePromotion(Id);
            if (res)
            {
                TempData["SuccessMessage"] = "Delete promotion success";
            }
            else
            {
                TempData["ErrorMessage"] = "Please delete promotion fail";
            }
            return RedirectToAction("Index", "TenantPromotion", new { id = tenantId });
        }

        public async Task<ActionResult> UpdateProMo(IFormFile upFile)
        {
            string? pmId = Request.Form["ProMoId"];
            int promoId = Convert.ToInt32(pmId);
            string? title = Request.Form["Title"];
            string? Percent = Request.Form["Percent"];
            string? Amount = Request.Form["Amount"];
            string? MinBill = Request.Form["MinBill"];
            decimal per = string.IsNullOrWhiteSpace(Percent) ? 0 : Convert.ToDecimal(Percent);
            decimal am = string.IsNullOrWhiteSpace(Amount) ? 0 : Convert.ToDecimal(Amount);
            decimal minB = string.IsNullOrWhiteSpace(MinBill) ? 0 : Convert.ToDecimal(MinBill);
            string? TenantId = Request.Form["TenantId"];
            int tenantId = Convert.ToInt32(TenantId);
            string? description = Request.Form["proDescription"];
            string? proStatus = Request.Form["ProMoStatus"];
            int status = Convert.ToInt32(proStatus);
            string? startStr = Request.Form["Start"];
            string? endStr = Request.Form["End"];
            DateTime startDate = DateTime.Parse(startStr);
            DateTime endDate = DateTime.Parse(endStr);
            if (startDate.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Start date must be today or in the future!";
                return RedirectToAction("Index", "TenantPromotion", new { id = tenantId });
            }

            if (startDate >= endDate)
            {
                TempData["ErrorMessage"] = "Start date must be before end date!";
                return RedirectToAction("Index", "TenantPromotion", new { id = tenantId });
            }
            if (per > 100)
            {
                TempData["ErrorMessage"] = "Discount percent must be < 100%!";
                return RedirectToAction("Index", "TenantPromotion", new { id = tenantId });
            }
            bool exists = await _tenantPromotionRepo.CheckPromotionAsync(title, tenantId, promoId);
            if (exists)
            {
                TempData["ErrorMessage"] = "Title already exist";
                return RedirectToAction("Index", "TenantPromotion", new { id = tenantId });
            }

            string fileName = "";
            string pathSave = Path.Combine(_webHostEnvironment.WebRootPath, "Content/Uploads/TenantPromotion");

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
                if (string.IsNullOrWhiteSpace(title))
                {
                    TempData["ErrorMessage"] = "Please enter enough information!";
                    return RedirectToAction("Index", "TenantPromotion", new { id = tenantId });
                }
                if (string.IsNullOrWhiteSpace(description))
                {
                    TempData["ErrorMessage"] = "Please enter enough information!";
                    return RedirectToAction("Index", "TenantPromotion", new { id = tenantId });
                }
                var entity = new TenantPromotion
                {
                    ID = promoId,
                    Title = title,
                    Img = fileName,
                    TenantId = tenantId,
                    Status = status,
                    Description = description,
                    DiscountPercent = per,
                    DiscountAmount = am,
                    MinBillAmount = minB,
                    Start = startDate,
                    End = endDate
                };

                bool result = await _tenantPromotionRepo.UpdatePromotion(entity);
                TempData["SuccessMessage"] = "Update Success!";

            }
            catch (Exception ex)
            {
                var error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "Error: " + error;
            }

            return RedirectToAction("Index", "TenantPromotion", new { id = tenantId });
        }
    }
}
