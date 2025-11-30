using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Admin.Models;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using Semester03.Services.Email;
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
        private readonly TicketRepository _ticketRepo;      
        private readonly TicketEmailService _emailService;

        public ShowtimesController(
            ShowtimeRepository showtimeRepo,
            TicketRepository ticketRepo,
            TicketEmailService emailService,
            AbcdmallContext context)
        {
            _showtimeRepo = showtimeRepo;
            _ticketRepo = ticketRepo;
            _emailService = emailService;
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
            var movie = await _context.TblMovies.FindAsync(tblShowtime.ShowtimeMovieId);
            if (movie != null)
            {
                if (tblShowtime.ShowtimeStart.Date < movie.MovieStartDate.Date ||
                    tblShowtime.ShowtimeStart.Date > movie.MovieEndDate.Date)
                {
                    TempData["Error"] = $"Invalid Date! The movie '{movie.MovieTitle}' is only available from {movie.MovieStartDate:dd/MM/yyyy} to {movie.MovieEndDate:dd/MM/yyyy}.";
                    return RedirectToAction(nameof(Index), new { date = tblShowtime.ShowtimeStart.Date });
                }
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
                    bool hasSoldTickets = await _context.TblTickets
                        .Include(t => t.TicketShowtimeSeat)
                        .AnyAsync(t => t.TicketShowtimeSeat.ShowtimeSeatShowtimeId == tblShowtime.ShowtimeId
                                       && t.TicketStatus == "sold");

                    if (hasSoldTickets)
                    {
                        // Lấy thông tin cũ để so sánh
                        var oldShowtime = await _showtimeRepo.GetByIdAsync(tblShowtime.ShowtimeId);


                        if (oldShowtime.ShowtimeMovieId != tblShowtime.ShowtimeMovieId ||
                            oldShowtime.ShowtimeStart != tblShowtime.ShowtimeStart ||
                            oldShowtime.ShowtimeScreenId != tblShowtime.ShowtimeScreenId ||
                            oldShowtime.ShowtimePrice != tblShowtime.ShowtimePrice)
                        {
                            TempData["Error"] = "Action Denied: Cannot change Movie, Time, or Screen because tickets have already been sold. Please cancel this showtime and create a new one.";
                            return RedirectToAction(nameof(Index), new { date = oldShowtime.ShowtimeStart.Date });
                        }
                        tblShowtime.ShowtimeMovieId = oldShowtime.ShowtimeMovieId;
                        tblShowtime.ShowtimeScreenId = oldShowtime.ShowtimeScreenId;
                        tblShowtime.ShowtimeStart = oldShowtime.ShowtimeStart;
                        tblShowtime.ShowtimePrice = oldShowtime.ShowtimePrice;
                    }
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

            bool hasSold = await _context.TblTickets
                .Include(t => t.TicketShowtimeSeat)
                .AnyAsync(t => t.TicketShowtimeSeat.ShowtimeSeatShowtimeId == id
                               && t.TicketStatus == "sold");

            if (hasSold)
            {
                return Json(new { success = false, message = "Cannot reschedule: Tickets have been sold." });
            }

            var showtime = await _showtimeRepo.GetByIdAsync(id);
            if (showtime == null) return Json(new { success = false, message = "Not found." });

            var newScreen = await _context.TblScreens.FindAsync(newScreenId);
            if (newScreen == null) return Json(new { success = false, message = "New screen not found." });

            int soldTickets = await _context.TblTickets
                .Include(t => t.TicketShowtimeSeat)
                .CountAsync(t => t.TicketShowtimeSeat.ShowtimeSeatShowtimeId == id
                                 && t.TicketStatus == "sold"); 

            // Nếu số vé đã bán > Tổng số ghế của phòng mới -> CHẶN
            if (soldTickets > newScreen.ScreenSeats)
            {
                return Json(new
                {
                    success = false,
                    message = $"Cannot move to '{newScreen.ScreenName}'! This showtime has sold {soldTickets} tickets, but the new screen only has {newScreen.ScreenSeats} seats."
                });
            }


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

            bool hasTickets = await _context.TblTickets
                .Include(t => t.TicketShowtimeSeat)
                .AnyAsync(t => t.TicketShowtimeSeat.ShowtimeSeatShowtimeId == id);

            if (hasTickets)
            {
                TempData["Error"] = "Cannot DELETE: Tickets exist (Active or Cancelled history). Please use 'Cancel Showtime' inside Details page to preserve history.";
                return RedirectToAction(nameof(Index), new { date = returnDate });
            }

            
            await _showtimeRepo.DeleteAsync(id);
            TempData["Success"] = "Showtime deleted.";
            return RedirectToAction(nameof(Index), new { date = returnDate });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelShowtime(int id)
        {
            var showtime = await _context.TblShowtimes.FindAsync(id);
            if (showtime == null) return NotFound();

            // 1. Check Showtime Time
            if (showtime.ShowtimeStart <= DateTime.Now)
            {
                TempData["Error"] = "Cannot cancel: Showtime has already started or ended.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // 2. Get Sold Tickets
            var soldTickets = await _context.TblTickets
                .Include(t => t.TicketBuyerUser)
                .Include(t => t.TicketShowtimeSeat)
                    .ThenInclude(ss => ss.ShowtimeSeatShowtime)
                        .ThenInclude(s => s.ShowtimeMovie)
                .Include(t => t.TicketShowtimeSeat).ThenInclude(ss => ss.ShowtimeSeatSeat)
                .Where(t => t.TicketShowtimeSeat.ShowtimeSeatShowtimeId == id && t.TicketStatus == "sold")
                .ToListAsync();

            // 3. Send Email
            if (soldTickets.Any())
            {
                var customerGroups = soldTickets.GroupBy(t => t.TicketBuyerUserId);
                foreach (var group in customerGroups)
                {
                    try
                    {
                        var userId = group.Key;
                        var first = group.First();
                        var movie = first.TicketShowtimeSeat?.ShowtimeSeatShowtime?.ShowtimeMovie?.MovieTitle;
                        var date = showtime.ShowtimeStart;
                        var seats = group.Select(t => t.TicketShowtimeSeat?.ShowtimeSeatSeat?.SeatLabel).ToList();
                        var refundAmount = group.Sum(t => t.TicketPrice);

                        await _emailService.SendMovieCancelEmailAsync(userId, movie, date, seats, refundAmount);
                    }
                    catch { /* Log error but continue */ }
                }
            }

            // 4. TRANSACTION: UPDATE DB (Cancel Ticket + Lock Seat)
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // A. Update Status -> "cancelled"
                    foreach (var t in soldTickets)
                    {
                        t.TicketStatus = "cancelled";
                        t.TicketUpdatedAt = DateTime.Now;
                    }
                    _context.TblTickets.UpdateRange(soldTickets);

                    // B. [MAIN LOGIC] Lock All Seat -> "blocked"
                    var allSeats = await _context.TblShowtimeSeats
                        .Where(ss => ss.ShowtimeSeatShowtimeId == id)
                        .ToListAsync();

                    foreach (var seat in allSeats)
                    {
                        seat.ShowtimeSeatStatus = "blocked"; 
                        seat.ShowtimeSeatReservedByUserId = null; 
                        seat.ShowtimeSeatReservedAt = null;
                        seat.ShowtimeSeatUpdatedAt = DateTime.Now;
                    }
                    _context.TblShowtimeSeats.UpdateRange(allSeats);

                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    TempData["Success"] = $"Showtime cancelled. {soldTickets.Count} tickets refunded. All seats are now BLOCKED.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["Error"] = "System Error: Could not cancel showtime. " + ex.Message;
                }
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}
