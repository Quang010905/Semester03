using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Admin.Models;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using System.Globalization;

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

        

        // GET: Admin/Showtimes
        public async Task<IActionResult> Index(DateTime? date)
        {
            //  Default to today if no date selected
            var selectedDate = date ?? DateTime.Now.Date;

            //  Get all screens (Rows of the timeline)
            var screens = await _context.TblScreens.OrderBy(s => s.ScreenName).ToListAsync();

            //  Get showtimes for that date
            var showtimes = await _showtimeRepo.GetShowtimesByDateAsync(selectedDate);

            // Get active movies
            var movies = await _context.TblMovies.Where(m => m.MovieStatus == 1).ToListAsync(); 

            //  Group data for the View
            var timelineData = new ShowtimeTimelineViewModel
            {
                SelectedDate = selectedDate,
                ScreenGroups = new List<ScreenTimelineGroup>(),
                AvailableMovies = movies
            };

            foreach (var screen in screens)
            {
                timelineData.ScreenGroups.Add(new ScreenTimelineGroup
                {
                    ScreenId = screen.ScreenId,
                    ScreenName = screen.ScreenName,
                    TotalSeats = screen.ScreenSeats,
                    Showtimes = showtimes.Where(s => s.ShowtimeScreenId == screen.ScreenId).ToList()
                });
            }

            return View(timelineData);
        }

        // GET: Admin/Showtimes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var showtime = await _showtimeRepo.GetByIdAsync(id.Value);
            if (showtime == null) return NotFound();
            return View(showtime);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveShowtime(
            [Bind("ShowtimeId,ShowtimeScreenId,ShowtimeMovieId,ShowtimePrice")] TblShowtime tblShowtime, string ShowtimeStart)
        {
            // Attempt flexible parsing
            if (DateTime.TryParse(ShowtimeStart, out DateTime parsedDate))
            {
                tblShowtime.ShowtimeStart = parsedDate;
            }
            else
            {
                TempData["Error"] = $"Invalid Date: {ShowtimeStart}. Please use yyyy-MM-ddTHH:mm format.";
                return RedirectToAction(nameof(Index));
            }

            int hour = tblShowtime.ShowtimeStart.Hour;
            if (tblShowtime.ShowtimeStart.TimeOfDay < new TimeSpan(8, 0, 0))
            {
                TempData["Error"] = "Showtime must start after 08:00 AM and before 12:00 AM..";
                return RedirectToAction(nameof(Index), new { date = tblShowtime.ShowtimeStart.Date });
            }

            if (tblShowtime.ShowtimeStart < DateTime.Now)
            {
                TempData["Error"] = "Cannot schedule a showtime in the past.";
                return RedirectToAction(nameof(Index), new { date = tblShowtime.ShowtimeStart.Date });
            }

            int duration = await _showtimeRepo.GetMovieDurationAsync(tblShowtime.ShowtimeMovieId);

            // Gọi hàm kiểm tra (Buffer 30 phút)
            bool isOverlapping = await _showtimeRepo.CheckOverlapAsync(
                tblShowtime.ShowtimeScreenId,
                tblShowtime.ShowtimeStart,
                duration,
                tblShowtime.ShowtimeId == 0 ? null : tblShowtime.ShowtimeId, // Exclude ID if Edit
                30 // 30 mins cleaning buffer
            );

            if (isOverlapping)
            {
                TempData["Error"] = "Conflict detected! This time slot overlaps with another showtime (including 30m cleaning time).";
                // Redirect back to the specific date so Admin sees the error
                return RedirectToAction(nameof(Index), new { date = tblShowtime.ShowtimeStart.Date });
            }

            // Bypass navigation validation
            ModelState.Remove("ShowtimeScreen");
            ModelState.Remove("ShowtimeMovie");
            ModelState.Remove("ShowtimeStart");

            if (ModelState.IsValid)
            {
                // 1. CREATE NEW
                if (tblShowtime.ShowtimeId == 0)
                {
                    await _showtimeRepo.AddAsync(tblShowtime);
                    TempData["Success"] = "Showtime created successfully.";
                }
                // 2. EDIT EXISTING
                else
                {
                    try
                    {
                        await _showtimeRepo.UpdateAsync(tblShowtime);
                        TempData["Success"] = "Showtime updated successfully.";
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        TempData["Error"] = "Error updating showtime.";
                    }
                }
            }
            else
            {
                TempData["Error"] = "Failed to save. Please check inputs.";
            }

            // Redirect back to the Timeline date
            return RedirectToAction(nameof(Index), new { date = tblShowtime.ShowtimeStart.Date });
        }

        // [POST] Admin/Showtimes/Reschedule
        // Called via AJAX when dragging an existing showtime on the timeline
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, int newScreenId, string newStartTime)
        {
            // Attempt flexible parsing
            if (!DateTime.TryParse(newStartTime, out DateTime parsedDate))
            {
                return Json(new { success = false, message = $"Invalid Date: {newStartTime}" });
            }

            if (parsedDate.TimeOfDay < new TimeSpan(8, 0, 0))
            {
                return Json(new { success = false, message = "Showtime must start after 08:00 AM and before 12:00 AM." });
            }

            if (parsedDate < DateTime.Now)
            {
                return Json(new { success = false, message = "Cannot move showtime to the past." });
            }

            var showtime = await _showtimeRepo.GetByIdAsync(id);
            if (showtime == null) return Json(new { success = false, message = "Not found." });

            // Update
            showtime.ShowtimeScreenId = newScreenId;
            showtime.ShowtimeStart = parsedDate;

            // Check Overlap (Optional but recommended)
            int duration = showtime.ShowtimeMovie.MovieDurationMin;
            bool isOverlapping = await _showtimeRepo.CheckOverlapAsync(newScreenId, parsedDate, duration, id, 30);

            if (isOverlapping) return Json(new { success = false, message = "Time Conflict!" });

            try
            {
                await _showtimeRepo.UpdateAsync(showtime);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Delete (Called from Modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFromCalendar(int id, DateTime returnDate)
        {
            var showtime = await _showtimeRepo.GetByIdAsync(id);
            if (showtime == null) return NotFound();
            if (showtime.ShowtimeStart < DateTime.Now)
            {
                TempData["Error"] = "Cannot delete a past or ongoing showtime.";
                return RedirectToAction(nameof(Index), new { date = returnDate });
            }

            // Check dependencies
            bool hasSeats = await _context.TblShowtimeSeats.AnyAsync(s => s.ShowtimeSeatShowtimeId == id && s.ShowtimeSeatStatus == "sold");
            if (hasSeats)
            {
                TempData["Error"] = "Cannot delete: Tickets have already been sold!";
                return RedirectToAction(nameof(Index), new { date = returnDate });
            }


            await _showtimeRepo.DeleteAsync(id);
            TempData["Success"] = "Showtime deleted.";
            return RedirectToAction(nameof(Index), new { date = returnDate });
        }

        
    }
}
