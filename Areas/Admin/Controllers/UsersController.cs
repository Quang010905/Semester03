using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class UsersController : Controller
    {
        private readonly UserRepository _userRepo;
        public UsersController(UserRepository userRepo)
        {
            _userRepo = userRepo;   
        }
        public async Task<IActionResult> IndexAsync()
        {
            var customers = await _userRepo.GetAllCustomersAsync();
            ViewBag.Customers = customers;


            var partners = await _userRepo.GetAllPartnersAsync();
            ViewBag.Partner = partners;

            return View();
        }
    }
}
