using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class RoleController : Controller
    {
        private readonly RoleRepository _roleRepo;
        public RoleController( RoleRepository roleRepo)
        {
            _roleRepo = roleRepo;
        }



        public async Task<IActionResult> IndexAsync()
        {


            var role = await _roleRepo.GetAllRolesAsync();
            ViewBag.Role = role;
            return View();
        }
    }
}
