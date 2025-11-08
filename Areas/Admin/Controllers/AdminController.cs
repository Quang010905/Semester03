using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Admin.Models;
using Semester03.Models.Entities;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize(Roles = "Super Admin, Mall Manager")] // Sẽ thêm khi làm Login
    public class AdminController : Controller
    {
        private readonly AbcdmallContext _context;
        public AdminController(AbcdmallContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = await _context.TblUsers.CountAsync(),
                TotalTenants = await _context.TblTenants.CountAsync(),
                TotalMovies = await _context.TblMovies.CountAsync(),
                TotalTicketsSold = await _context.TblTickets.CountAsync()
            };

            return View(viewModel);
        }
    }
}
