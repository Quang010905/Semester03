using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using System.Security.Claims;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class ProfileController : ClientBaseController
    {
        private readonly UserRepository _userRepo;
        private readonly UserActivityRepository _activityRepo;

        public ProfileController(
            TenantTypeRepository tenantTypeRepo,  // 👈 thêm vào để truyền cho base
            UserRepository userRepo,
            UserActivityRepository activityRepo
        ) : base(tenantTypeRepo)                 // 👈 gọi constructor cha
        {
            _userRepo = userRepo;
            _activityRepo = activityRepo;
        }


        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // Lấy user ID từ Claims
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Client" });
            }

            // Lấy user từ DB
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Lấy lịch sử vé & khiếu nại
            var tickets = _activityRepo.GetTicketHistory(userId);
            var complaints = _activityRepo.GetComplaintHistory(userId);

            // Trả về ViewModel
            var vm = new ProfileViewModel
            {
                User = user,
                TicketHistory = tickets,
                ComplaintHistory = complaints
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
        {
            // Lấy userId từ Claims (giống action Profile)
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new
                {
                    success = false,
                    message = "You are not logged in."
                });
            }

            if (model == null)
            {
                return Json(new
                {
                    success = false,
                    message = "The data submitted is invalid."
                });
            }

            var (success, error) = await _userRepo.UpdateProfileAsync(
                userId,
                model.FullName,
                model.Email,
                model.Phone,
                model.NewPassword
            );

            if (!success)
            {
                return Json(new
                {
                    success = false,
                    message = error ?? "Update failed."
                });
            }

            return Json(new
            {
                success = true,
                message = "Profile updated successfully."
            });
        }
    }

}

