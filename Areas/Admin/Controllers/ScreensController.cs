using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Admin.Models;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class ScreensController : Controller
    {

        // This is the hard-coded ID of your one-and-only Cinema
        private const int FIXED_CINEMA_ID = 1;

        private readonly ScreenRepository _screenRepo;
        private readonly SeatRepository _seatRepo; // <-- Add SeatRepo
        private readonly AbcdmallContext _context; // <-- Add Context

        // Inject all needed repositories
        public ScreensController(ScreenRepository screenRepo, SeatRepository seatRepo, AbcdmallContext context)
        {
            _screenRepo = screenRepo;
            _seatRepo = seatRepo;
            _context = context;
        }

        // GET: Admin/Screens
        public async Task<IActionResult> Index()
        {
            var screens = await _screenRepo.GetAllAsync();
            return View(screens);
        }

        // GET: Admin/Screens/Details/5
        public async Task<IActionResult> Details(int id)
        {
            // 1. Get the Screen with all its seats
            var screen = await _context.TblScreens
                .Include(s => s.ScreenCinema)
                .Include(s => s.TblSeats.OrderBy(seat => seat.SeatRow).ThenBy(seat => seat.SeatCol)) //
                .FirstOrDefaultAsync(m => m.ScreenId == id);

            if (screen == null) return NotFound();

            // 2. Prepare the simplified ViewModel
            var viewModel = new ScreenDetailViewModel
            {
                Screen = screen,
                BatchForm = new BatchCreateSeatVm { ScreenId = id }
            };

            // 3. We no longer need 'editSeatId' or 'SeatToEdit'
            return View(viewModel);
        }

        

        // GET: Admin/Screens/Create
        public IActionResult Create()
        {
            // We don't need a dropdown, we will set the Cinema ID automatically
            return View();
        }

        // POST: Admin/Screens/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ScreenName,ScreenSeats")] TblScreen tblScreen)
        {
            ModelState.Remove("ScreenCinemaId");
            ModelState.Remove("ScreenCinema");

            // --- Business Logic Validation ---
            if (tblScreen.ScreenSeats <= 0)
            {
                ModelState.AddModelError("ScreenSeats", "Total Seats must be greater than 0.");
            }
            // --- END VALIDATION ---

            // Re-check model state after adding the ID
            if (ModelState.IsValid)
            {
                // Manually set the Cinema ID
                tblScreen.ScreenCinemaId = FIXED_CINEMA_ID;
                await _screenRepo.AddAsync(tblScreen);
                return RedirectToAction(nameof(Index));
            }
            return View(tblScreen);
        }

        // GET: Admin/Screens/Edit/5
        public async Task<IActionResult> Edit(int? id, string returnUrl) // 1. Add returnUrl
        {
            if (id == null) return NotFound();
            var screen = await _screenRepo.GetByIdAsync(id.Value);
            if (screen == null) return NotFound();

            // 2. Pass the returnUrl to the View
            ViewData["ReturnUrl"] = returnUrl;
            return View(screen);
        }

        // POST: Admin/Screens/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("ScreenId,ScreenName,ScreenSeats")] TblScreen tblScreen,
            string returnUrl)
        {
            ModelState.Remove("ScreenCinemaId");
            ModelState.Remove("ScreenCinema");
            if (id != tblScreen.ScreenId) return NotFound();

            // --- Business Logic Validation ---
            if (tblScreen.ScreenSeats <= 0)
            {
                ModelState.AddModelError("ScreenSeats", "Total Seats must be greater than 0.");
            }

            // ADVANCED CHECK: Prevent reducing capacity below current defined seats
            var currentSeatCount = await _context.TblSeats.CountAsync(s => s.SeatScreenId == id);
            if (tblScreen.ScreenSeats < currentSeatCount)
            {
                ModelState.AddModelError("ScreenSeats", $"Cannot set capacity to {tblScreen.ScreenSeats}. This screen already has {currentSeatCount} seats defined.");
            }
            // --- END VALIDATION ---

            if (ModelState.IsValid)
            {
                try
                {
                    // Manually set the Cinema ID again to ensure it's correct
                    tblScreen.ScreenCinemaId = FIXED_CINEMA_ID;
                    await _screenRepo.UpdateAsync(tblScreen);
                }
                catch (DbUpdateConcurrencyException)
                {
                    var exists = await _screenRepo.GetByIdAsync(id);
                    if (exists == null) return NotFound();
                    else throw;
                }
                return RedirectToLocal(returnUrl);
            }
            return View(tblScreen);
        }

        // GET: Admin/Screens/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var screen = await _screenRepo.GetByIdAsync(id.Value);
            if (screen == null) return NotFound();

            // === Dependency Check ===
            // Check if any child records exist
            bool hasSeats = await _context.TblSeats.AnyAsync(s => s.SeatScreenId == id.Value);
            bool hasShowtimes = await _context.TblShowtimes.AnyAsync(s => s.ShowtimeScreenId == id.Value);

            if (hasSeats || hasShowtimes)
            {
                // If dependencies exist, pass an error message to the View
                ViewData["HasDependencies"] = true;
                ViewData["ErrorMessage"] = "This screen cannot be deleted because it has existing Seats or Showtimes linked to it. Please delete those first.";
            }
            else
            {
                ViewData["HasDependencies"] = false;
            }
            // === END OF CHECK ===

            return View(screen);
        }

        // POST: Admin/Screens/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // === Final Dependency Check (Safety Net) ===
            bool hasSeats = await _context.TblSeats.AnyAsync(s => s.SeatScreenId == id);
            bool hasShowtimes = await _context.TblShowtimes.AnyAsync(s => s.ShowtimeScreenId == id);

            if (hasSeats || hasShowtimes)
            {
                // Cannot delete, send error message
                TempData["Error"] = "This screen cannot be deleted because it has existing Seats or Showtimes. You must delete them first.";
                return RedirectToAction(nameof(Index));
            }
            // === END OF CHECK ===

            // If no dependencies, proceed with delete
            await _screenRepo.DeleteAsync(id);
            TempData["Success"] = "Screen deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                // If the returnUrl is local (within your site), redirect to it
                return Redirect(returnUrl);
            }
            else
            {
                // Otherwise, redirect to a safe default page (e.g., Index)
                return RedirectToAction(nameof(Index));
            }
        }

    }
}
