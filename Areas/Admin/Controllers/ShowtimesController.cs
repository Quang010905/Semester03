using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class ShowtimesController : Controller
    {
        private readonly ShowtimeRepository _showtimeRepo;
        // We inject the context to get lists for Dropdowns
        private readonly AbcdmallContext _context;

        public ShowtimesController(ShowtimeRepository showtimeRepo, AbcdmallContext context)
        {
            _showtimeRepo = showtimeRepo;
            _context = context;
        }

        // --- Helper method to populate dropdowns ---
        private void PopulateDropdowns(object selectedMovie = null, object selectedScreen = null)
        {
            // Get Movie list
            ViewData["MovieList"] = new SelectList(
                _context.TblMovies.Where(m => m.MovieStatus == 1).OrderBy(m => m.MovieTitle),
                "MovieId", "MovieTitle", selectedMovie);

            // Get Screen list
            ViewData["ScreenList"] = new SelectList(
                _context.TblScreens.OrderBy(s => s.ScreenName),
                "ScreenId", "ScreenName", selectedScreen);
        }

        // GET: Admin/Showtimes
        public async Task<IActionResult> Index()
        {
            var showtimes = await _showtimeRepo.GetAllAsync();
            return View(showtimes);
        }

        // GET: Admin/Showtimes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var showtime = await _showtimeRepo.GetByIdAsync(id.Value);
            if (showtime == null) return NotFound();
            return View(showtime);
        }

        // GET: Admin/Showtimes/Create
        public IActionResult Create()
        {
            // Populate the dropdowns for the Create form
            PopulateDropdowns();
            return View();
        }

        // POST: Admin/Showtimes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ShowtimeScreenId,ShowtimeMovieId,ShowtimeStart,ShowtimePrice")] TblShowtime tblShowtime)
        {
            // Remove validation for navigation properties
            ModelState.Remove("ShowtimeScreen");
            ModelState.Remove("ShowtimeMovie");

            // --- Business Logic Validation ---
            if (tblShowtime.ShowtimePrice <= 0)
            {
                ModelState.AddModelError("ShowtimePrice", "Price must be greater than 0.");
            }
            if (tblShowtime.ShowtimeStart < DateTime.Now)
            {
                ModelState.AddModelError("ShowtimeStart", "Start Time cannot be in the past.");
            }
            // --- END VALIDATION ---

            if (ModelState.IsValid)
            {
                await _showtimeRepo.AddAsync(tblShowtime);
                TempData["Success"] = "Showtime created successfully. Seats were generated automatically.";
                return RedirectToAction(nameof(Index));
            }

            // If invalid, re-populate the dropdowns and return
            PopulateDropdowns(tblShowtime.ShowtimeMovieId, tblShowtime.ShowtimeScreenId);
            return View(tblShowtime);
        }

        // GET: Admin/Showtimes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var showtime = await _showtimeRepo.GetByIdAsync(id.Value);
            if (showtime == null) return NotFound();

            // Populate the dropdowns for the Edit form
            PopulateDropdowns(showtime.ShowtimeMovieId, showtime.ShowtimeScreenId);
            return View(showtime);
        }

        // POST: Admin/Showtimes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("ShowtimeId,ShowtimeScreenId,ShowtimeMovieId,ShowtimeStart,ShowtimePrice")] TblShowtime tblShowtime)
        {
            if (id != tblShowtime.ShowtimeId) return NotFound();

            ModelState.Remove("ShowtimeScreen");
            ModelState.Remove("ShowtimeMovie");

            // --- Business Logic Validation ---
            if (tblShowtime.ShowtimePrice <= 0)
            {
                ModelState.AddModelError("ShowtimePrice", "Price must be greater than 0.");
            }
            if (tblShowtime.ShowtimeStart < DateTime.Now)
            {
                ModelState.AddModelError("ShowtimeStart", "Start Time cannot be in the past.");
            }
            // --- END VALIDATION ---

            if (ModelState.IsValid)
            {
                try
                {
                    await _showtimeRepo.UpdateAsync(tblShowtime);
                }
                catch (DbUpdateConcurrencyException)
                {
                    var exists = await _showtimeRepo.GetByIdAsync(id);
                    if (exists == null) return NotFound();
                    else throw;
                }
                TempData["Success"] = "Showtime updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns(tblShowtime.ShowtimeMovieId, tblShowtime.ShowtimeScreenId);
            return View(tblShowtime);
        }

        // GET: Admin/Showtimes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var showtime = await _showtimeRepo.GetByIdAsync(id.Value);
            if (showtime == null) return NotFound();

            // Check for dependencies (Tbl_ShowtimeSeat / Tbl_Ticket)
            bool hasSeats = await _context.TblShowtimeSeats.AnyAsync(s => s.ShowtimeSeatShowtimeId == id);
            if (hasSeats)
            {
                ViewData["HasDependencies"] = true;
                ViewData["ErrorMessage"] = "This showtime cannot be deleted. It has existing seats (and potential tickets) linked to it.";
            }

            return View(showtime);
        }

        // POST: Admin/Showtimes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Final check
            bool hasSeats = await _context.TblShowtimeSeats.AnyAsync(s => s.ShowtimeSeatShowtimeId == id);
            if (hasSeats)
            {
                TempData["Error"] = "Cannot delete: This showtime has seats or tickets linked to it.";
                return RedirectToAction(nameof(Index));
            }

            await _showtimeRepo.DeleteAsync(id);
            TempData["Success"] = "Showtime deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
