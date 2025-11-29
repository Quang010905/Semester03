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
            TenantTypeRepository tenantTypeRepo,
            UserRepository userRepo,
            UserActivityRepository activityRepo
        ) : base(tenantTypeRepo)
        {
            _userRepo = userRepo;
            _activityRepo = activityRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Client" });
            }

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var tickets = _activityRepo.GetTicketHistory(userId);
            var complaints = _activityRepo.GetComplaintHistory(userId);

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
