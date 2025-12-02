using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Admin.Models;
using Semester03.Models.Repositories;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Semester03.Models.Entities;
using System.ComponentModel.DataAnnotations;


namespace Semester03.Areas.Partner.Controllers
{
    [Area("Partner")]
    [Authorize(Roles = "2")]
    public class ShopController : Controller
    {
        private readonly UserRepository _userRepo;
        private readonly TenantRepository _tenantRepo;
        private readonly TenantTypeRepository _tenantTypeRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ShopController(IWebHostEnvironment webHostEnvironment, UserRepository userRepo,  TenantRepository tenantRepo, TenantTypeRepository tenantTypeRepo)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _tenantRepo = tenantRepo ?? throw new ArgumentException(nameof(_tenantRepo));
            _tenantTypeRepo = tenantTypeRepo ?? throw new ArgumentNullException(nameof(_tenantTypeRepo));
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index(int page = 1, string search = "")
        {
            const int pageSize = 10;
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "User is not logged in.";
                return RedirectToAction("Login", "Account");
            }

            int userId = int.Parse(userIdString);
            var list = await _tenantRepo.GetTenantByUserId(userId);

            string normalizedSearch = _tenantRepo.NormalizeSearch(search);
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                list = list
                    .Where(t => _tenantRepo.NormalizeSearch(t.Name).Contains(normalizedSearch))
                    .ToList();
            }

            var totalItems = list.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var data = list
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var item = await _userRepo.CreateTenantByUserId(userId);
            var lsTenantType = await _tenantTypeRepo.GetActiveTenantType();
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
        public async Task<ActionResult> Edit(int id)
        {
            var itemTenant = await _tenantRepo.FindById(id);
            var lsTenantType = await _tenantTypeRepo.GetActiveTenantType();
            ViewBag.itemTenant = itemTenant;
            ViewBag.listTenantType = lsTenantType;
            return View();
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
            string pathSave = Path.Combine(_webHostEnvironment.WebRootPath, "Content/Uploads/Tenant");

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
                    return RedirectToAction("Edit", "Shop", new { id = tenantId });
                }

                if (string.IsNullOrWhiteSpace(typeId) || typeId == "0")
                {
                    TempData["ErrorMessage"] = "Please enter enough information!";
                    return RedirectToAction("Edit", "Shop", new { id = tenantId });
                }

                bool exists = await _tenantRepo.CheckTenantNameAsync(tenantName, tenantId);
                if (exists)
                {
                    TempData["ErrorMessage"] = "Tenant name already exist";
                    return RedirectToAction("Edit", "Shop", new { id = tenantId });
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
                TempData["SuccessMessage"] = "Update tenant success!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }

            return RedirectToAction("Index", "Shop");
        }
        // ===================== RESET PASSWORD CHO PARTNER =====================
        public class PartnerResetPasswordVm
        {
            [Required]
            [DataType(DataType.Password)]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required]
            [MinLength(6, ErrorMessage = "New password must be at least 6 characters.")]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "Confirm password does not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View(new PartnerResetPasswordVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(PartnerResetPasswordVm model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Lấy id user đang đăng nhập
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "You are not logged in.";
                return RedirectToAction("Login", "Account", new { area = "Client" });
            }

            int userId = int.Parse(userIdString);
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return View(model);
            }

            var hasher = new PasswordHasher<TblUser>();

            // kiểm tra mật khẩu hiện tại
            var verifyResult = hasher.VerifyHashedPassword(
                user,
                user.UsersPassword ?? "",
                model.CurrentPassword
            );

            if (verifyResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                return View(model);
            }

            // Hash và lưu mật khẩu mới
            user.UsersPassword = hasher.HashPassword(user, model.NewPassword);
            user.UsersUpdatedAt = DateTime.Now;
            await _userRepo.UpdateAsync(user);

            ViewBag.Success = "Password has been changed successfully.";
            ModelState.Clear();

            return View(new PartnerResetPasswordVm());
        }


    }
}
