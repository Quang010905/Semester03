using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UsersController : Controller
    {
        private readonly UserRepository _userRepo;
        private readonly RoleRepository _roleRepo;
        private readonly PasswordHasher<TblUser> _hasher = new PasswordHasher<TblUser>();

        public UsersController(UserRepository userRepo, RoleRepository roleRepo)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
        }

        // GET: Index
        public async Task<IActionResult> Index()
        {
            var customers = await _userRepo.GetAllCustomersAsync();
            ViewBag.Customers = customers;

            var partners = await _userRepo.GetAllPartnersAsync();
            ViewBag.Partner = partners;

            var admin = await _userRepo.GetAllAdminAsync();
            ViewBag.Admin = admin;

            var inactive = await _userRepo.GetInactiveAccountAsync();
            ViewBag.Inactive = inactive; 

            return View();
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid user ID.";
                return RedirectToAction("Index");
            }

            var user = await _userRepo.GetByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = $"User with ID {id} not found.";
                return RedirectToAction("Index");
            }

            // Lấy danh sách roles
            var roles = await _roleRepo.GetAllRolesAsync();
            ViewBag.Roles = new SelectList(
                roles,
                "RolesId",        // Primary key
                "RolesName",      // Display text
                user.UsersRoleId  // Selected value
            );

            return View(user);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TblUser model)
        {
            ModelState.Remove("UsersRole");

            // Nếu model không hợp lệ, reload roles và trả lại view
            if (!ModelState.IsValid)
            {
                var roles = await _roleRepo.GetAllRolesAsync();
                ViewBag.Roles = new SelectList(roles, "RolesId", "RolesName", model.UsersRoleId);
                return View(model);
            }

            try
            {
                // Lấy user từ database
                var user = await _userRepo.GetByIdAsync(model.UsersId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index");
                }

                // Kiểm tra username có bị trùng không (ngoại trừ chính nó)
                var existingUserByUsername = await _userRepo.GetByUsernameAsync(model.UsersUsername);
                if (existingUserByUsername != null && existingUserByUsername.UsersId != model.UsersId)
                {
                    ModelState.AddModelError("UsersUsername", "Username already exists.");
                    var roles = await _roleRepo.GetAllRolesAsync();
                    ViewBag.Roles = new SelectList(roles, "RolesId", "RolesName", model.UsersRoleId);
                    return View(model);
                }

                // Lưu role cũ để kiểm tra xem có thay đổi không
                var oldRoleId = user.UsersRoleId;

                // Cập nhật thông tin
                user.UsersUsername = model.UsersUsername?.Trim();
                user.UsersFullName = model.UsersFullName?.Trim();
                user.UsersEmail = model.UsersEmail?.Trim();
                user.UsersPhone = model.UsersPhone?.Trim();
                user.UsersPoints = model.UsersPoints;
                user.UsersStatus = model.UsersStatus;
                user.UsersRoleId = model.UsersRoleId;
                
                // Nếu role thay đổi và có lý do thì lưu lý do
                if (oldRoleId != model.UsersRoleId && !string.IsNullOrWhiteSpace(model.UsersRoleChangeReason))
                {
                    user.UsersRoleChangeReason = model.UsersRoleChangeReason.Trim();
                }
                
                user.UsersUpdatedAt = DateTime.Now;

                // Lưu vào database
                await _userRepo.UpdateAsync(user);

                TempData["SuccessMessage"] = $"User '{user.UsersUsername}' updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log error (nên dùng ILogger)
                ModelState.AddModelError("", $"Error updating user: {ex.Message}");
                
                var roles = await _roleRepo.GetAllRolesAsync();
                ViewBag.Roles = new SelectList(roles, "RolesId", "RolesName", model.UsersRoleId);
                return View(model);
            }
        }

        // GET: Delete (Optional - Soft delete)
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid user ID.";
                return RedirectToAction("Index");
            }

            var user = await _userRepo.GetByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }

            return View(user);
        }

        // POST: Delete (Soft delete - set status to inactive)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index");
                }

                // Soft delete: set status to 0 (inactive)
                user.UsersStatus = 0;
                user.UsersUpdatedAt = DateTime.Now;
                await _userRepo.UpdateAsync(user);

                TempData["SuccessMessage"] = $"User '{user.UsersUsername}' has been deactivated.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
                return RedirectToAction("Index");
            }
        }



        public ActionResult RestoreUser(int id)
        {
            _userRepo.RestoreUserStatus(id);
            return RedirectToAction("Index");
        }








        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePartner(TblUser model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please check your input!";
                return RedirectToAction("Index");
            }

            // ⭐ HASH PASSWORD TẠI ĐÂY ⭐
            model.UsersPassword = _hasher.HashPassword(model, model.UsersPassword);

            bool result = _userRepo.AddPartner(model);

            if (result)
                TempData["Success"] = "Partner created successfully!";
            else
                TempData["Error"] = "Failed to create partner!";

            return RedirectToAction("Index");
        }

        // ===================== RESET PASSWORD =====================
        public async Task<IActionResult> ResetPassword(int id)
        {
            if (id <= 0)
                return RedirectToAction("Index");

            var user = await _userRepo.GetByIdAsync(id);
            if (user == null)
                return RedirectToAction("Index");

           
            string newPassword = "password123";
            user.UsersPassword = _hasher.HashPassword(user, newPassword);
            user.UsersUpdatedAt = DateTime.Now;

            await _userRepo.UpdateAsync(user);

        
            return RedirectToAction("Index");
        }





    }
}