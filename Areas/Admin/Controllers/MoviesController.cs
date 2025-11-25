// File: Areas/Admin/Controllers/MoviesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    // [Authorize(Roles = "Super Admin, Mall Manager")]
    public class MoviesController : Controller
    {
        private readonly MovieRepository _movieRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly AbcdmallContext _context;

        public MoviesController(MovieRepository movieRepo,
                                        IWebHostEnvironment webHostEnvironment,
                                        AbcdmallContext context) 
        {
            _movieRepo = movieRepo;
            _webHostEnvironment = webHostEnvironment;
            _context = context; 
        }
        // GET: Admin/Movies
        public async Task<IActionResult> Index()
        {
            var movies = await _movieRepo.GetAllAsync();
            var today = DateTime.Now;
            int expiredCount = movies.Count(m => m.MovieStatus == 1 && m.MovieEndDate < today);

            ViewData["ExpiredCount"] = expiredCount;
            return View(movies);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, int currentStatus)
        {
            // Swap status: If 1 -> 0, If 0 -> 1
            int newStatus = (currentStatus == 1) ? 0 : 1;

            var result = await _movieRepo.UpdateStatusAsync(id, newStatus);

            if (result)
            {
                string statusName = newStatus == 1 ? "Available" : "Unavailable";
                TempData["Success"] = $"Movie status changed to {statusName}.";
            }
            else
            {
                TempData["Error"] = "Movie not found.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var movie = await _movieRepo.GetByIdAsync(id.Value);
            if (movie == null) return NotFound();
            return View(movie);
        }

        // GET: Admin/Movies/Create
        public IActionResult Create()
        {
            
            // We must round the time to avoid the browser step validation error.
            var now = DateTime.Now;

            // Round down to the current minute (e.g., 4:31:53 -> 4:31:00)
            var defaultStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

            var model = new TblMovie
            {
                MovieStatus = 1, // Default to 'Available'

                // Use the "clean" (rounded) values
                MovieStartDate = defaultStart,
                MovieEndDate = defaultStart.AddDays(14)
            };
            return View(model);
        }

        // [POST] Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            // 1. FIXED: Removed 'MovieRate' from Bind.
            //    We will set it manually to 0.
            [Bind("MovieTitle,MovieGenre,MovieDirector,MovieStartDate,MovieEndDate,MovieDurationMin,MovieDescription,MovieStatus")] TblMovie tblMovie,
            IFormFile? imageFile)
        {
            // 2. FIXED: Remove the 'MovieImg' validation first
            ModelState.Remove("MovieImg");

            // 3. Add our custom validation for the file
            if (imageFile == null)
            {
                // This error will be shown by the span for "MovieImg" 
                ModelState.AddModelError("MovieImg", "A poster image is required.");
            }

            // --- Business Logic Validation ---
            if (tblMovie.MovieDurationMin <= 0)
            {
                ModelState.AddModelError("MovieDurationMin", "Duration must be greater than 0 minutes.");
            }
            if (tblMovie.MovieStartDate.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("MovieStartDate", "Start Date cannot be in the past.");
            }
            if (tblMovie.MovieEndDate <= tblMovie.MovieStartDate)
            {
                ModelState.AddModelError("MovieEndDate", "End Date must be after the Start Date.");
            }
            // --- END VALIDATION ---

            // 4. Now, check ModelState
            if (ModelState.IsValid)
            {
                if (imageFile != null)
                {
                    // Save file and get ONLY the filename
                    tblMovie.MovieImg = await SaveImageFileAsync(imageFile);
                }

                // 5. FIXED: Set default Rating to 0
                tblMovie.MovieRate = 0; // Default to 0 stars (Not Rated)

                await _movieRepo.AddAsync(tblMovie);
                return RedirectToAction(nameof(Index));
            }

            // If we are here, something failed (e.g., no image, or title is missing)
            return View(tblMovie);
        }

        // GET: Admin/Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var movie = await _movieRepo.GetByIdAsync(id.Value);
            if (movie == null) return NotFound();
            return View(movie);
        }

        // [POST] Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            // FIXED: Bind all fields, including the ID and old MovieImg
            [Bind("MovieId,MovieTitle,MovieGenre,MovieDirector,MovieImg,MovieStartDate,MovieEndDate,MovieDurationMin,MovieDescription,MovieStatus,MovieRate")] TblMovie tblMovie,
            IFormFile? imageFile)
        {
            if (id != tblMovie.MovieId) return NotFound();

            // --- Business Logic Validation ---
            if (tblMovie.MovieDurationMin <= 0)
            {
                ModelState.AddModelError("MovieDurationMin", "Duration must be greater than 0 minutes.");
            }
            if (tblMovie.MovieEndDate <= tblMovie.MovieStartDate)
            {
                ModelState.AddModelError("MovieEndDate", "End Date must be after the Start Date.");
            }
            if (tblMovie.MovieRate < 0 || tblMovie.MovieRate > 5)
            {
                ModelState.AddModelError("MovieRate", "Rating must be between 0 and 5.");
            }
            // --- END VALIDATION ---

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null)
                    {
                    // A new file was uploaded
                         if (!string.IsNullOrEmpty(tblMovie.MovieImg))
                        {
                            DeleteImageFile(tblMovie.MovieImg);
                        }

                        // Save the new file and update the file name
                        tblMovie.MovieImg = await SaveImageFileAsync(imageFile);
                    }
                    // If imageFile is null, tblMovie.MovieImg (from the hidden field) 
                    // keeps its old value. This is correct.

                    await _movieRepo.UpdateAsync(tblMovie);
                }
                catch (DbUpdateConcurrencyException)
                {
                    var exists = await _movieRepo.GetByIdAsync(id);
                    if (exists == null) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tblMovie);
        }

        private async Task<string> SaveImageFileAsync(IFormFile imageFile)
        {
            // 1. Get the wwwroot path
            string wwwRootPath = _webHostEnvironment.WebRootPath;

            // 2. Define the save path (wwwroot/Content/Uploads/Movies)
            string savePath = Path.Combine(wwwRootPath, "Content", "Uploads", "Movies");

            // 3. Create the directory if it doesn't exist
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            // 4. Create a unique file name
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);

            // 5. Create the full file path
            string filePath = Path.Combine(savePath, fileName);

            // 6. Save the file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            // 7. Return ONLY the file name
            return fileName;
        }

        // GET: Admin/Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var movie = await _movieRepo.GetByIdAsync(id.Value);
            if (movie == null) return NotFound();
            
            // --- ADDED: Dependency Check ---
            // Check if any Showtimes are linked to this Movie
            bool hasShowtimes = await _context.TblShowtimes.AnyAsync(s => s.ShowtimeMovieId == id.Value);
            bool hasComplaints = await _context.TblCustomerComplaints.AnyAsync(c => c.CustomerComplaintMovieId == id.Value);

            if (hasShowtimes || hasComplaints)
            {
                ViewData["HasDependencies"] = true;
                string error = "This movie cannot be deleted. It is linked to:";
                if (hasShowtimes) error += " one or more Showtimes.";
                if (hasComplaints) error += " one or more Customer Reviews.";
                ViewData["ErrorMessage"] = error;
            }
            else
            {
                ViewData["HasDependencies"] = false;
            }
            // --- END OF CHECK ---

            return View(movie);
        }

        // POST: Admin/Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // --- Final Dependency Check (Safety Net) ---
            bool hasShowtimes = await _context.TblShowtimes.AnyAsync(s => s.ShowtimeMovieId == id);
            bool hasComplaints = await _context.TblCustomerComplaints.AnyAsync(c => c.CustomerComplaintMovieId == id); //

            if (hasShowtimes || hasComplaints)
            {
                TempData["Error"] = "This movie cannot be deleted (it has dependencies).";
                return RedirectToAction(nameof(Index));
            }
            // --- END OF CHECK ---

            // 1. Get the movie object first to find the filename
            var movie = await _movieRepo.GetByIdAsync(id);
            if (movie == null)
            {
                return RedirectToAction(nameof(Index));
            }

            // 2. Store the filename before deleting the DB record
            string oldImageFileName = movie.MovieImg;

            // 3. Delete the record from the database
            await _movieRepo.DeleteAsync(id);

            // 4. Delete the physical file (if it exists)
            if (!string.IsNullOrEmpty(oldImageFileName))
            {
                DeleteImageFile(oldImageFileName);
            }

            TempData["Success"] = "Movie deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        private void DeleteImageFile(string fileName)
        {
            try
            {
                // 1. Get the wwwroot path
                string wwwRootPath = _webHostEnvironment.WebRootPath;

                // 2. Define the full file path
                string filePath = Path.Combine(wwwRootPath, "Content", "Uploads", "Movies", fileName);

                // 3. Check if the file exists and delete it
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                // Optional: Log an error if file deletion fails
                // (e.g., file is locked)
                Console.WriteLine($"Error deleting file: {fileName}. Error: {ex.Message}");
            }
        }
    }
}