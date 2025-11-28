using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class UsersController : Controller
    {
        private readonly UserRepository _userRepo;
        private readonly RoleRepository _roleRepo; 
        public UsersController(UserRepository userRepo, RoleRepository roleRepo)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
        }
        public async Task<IActionResult> Index()
        {
            var customers = await _userRepo.GetAllCustomersAsync();
            ViewBag.Customers = customers;


            var partners = await _userRepo.GetAllPartnersAsync();
            ViewBag.Partner = partners;

            return View();
        }



        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userRepo.GetByIdAsync(id); 
            if (user == null)
            {
                return NotFound();
            }


            var roles = await _roleRepo.GetAllRolesAsync(); // Lấy danh sách role
            ViewBag.Roles = new SelectList(roles, "RoleId", "RoleName", user.UsersRoleId);

            return View(user); 
        }




        [HttpPost]
        public async Task<IActionResult> Edit(TblUser model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userRepo.GetByIdAsync(model.UsersId);
            if (user == null) return NotFound();


            // Cập nhật thông tin
            user.UsersUsername = model.UsersUsername;
            user.UsersFullName = model.UsersFullName;
            user.UsersEmail = model.UsersEmail;
            user.UsersPhone = model.UsersPhone;
            user.UsersPoints = model.UsersPoints;
            user.UsersStatus = model.UsersStatus;
            user.UsersRoleId = model.UsersRoleId;
            user.UsersRoleChangeReason = model.UsersRoleChangeReason;
            user.UsersUpdatedAt = DateTime.Now;

            await _userRepo.UpdateAsync(user);

            return RedirectToAction("Index");
        }



    }
}
