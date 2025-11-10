// File: Areas/Admin/Controllers/MoviesController.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Repositories;
using Semester03.Models.Entities;

// Add these 'using' statements for file upload
using Microsoft.AspNetCore.Http; // For IFormFile
using System.IO; // For Path
using System; // For Guid
using System.Threading.Tasks; // For Task

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize(Roles = "Super Admin, Mall Manager")]
    public class MoviesController : Controller
    {
        private readonly MovieRepository _movieRepo;
        // 1. Add IWebHostEnvironment
        private readonly IWebHostEnvironment _webHostEnvironment;

        // 2. Inject it in the constructor
        public MoviesController(MovieRepository movieRepo, IWebHostEnvironment webHostEnvironment)
        {
            _movieRepo = movieRepo;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Movies
        public async Task<IActionResult> Index()
        {
            var movies = await _movieRepo.GetAllAsync();
            return View(movies);
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
            var model = new TblMovie
            {
                MovieStatus = 1, // Default to 'Available'
                MovieStartDate = DateTime.Now,
                MovieEndDate = DateTime.Now.AddDays(14)
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
            [Bind("MovieId,MovieTitle,MovieGenre,MovieDirector,MovieImg,MovieStartDate,MovieEndDate,MovieDurationMin,MovieDescription,MovieStatus")] TblMovie tblMovie,
            IFormFile? imageFile)
        {
            if (id != tblMovie.MovieId) return NotFound();

            if (ModelState.IsValid)
            {
                tblMovie.MovieRate = 0;
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

            // 2. Define the save path (wwwroot/Admin/img)
            string savePath = Path.Combine(wwwRootPath, "Admin", "img");

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
            return View(movie);
        }

        // POST: Admin/Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // 1. Get the movie object first to find the filename
            var movie = await _movieRepo.GetByIdAsync(id);
            if (movie == null)
            {
                // This shouldn't happen if the GET Delete page worked
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

            return RedirectToAction(nameof(Index));
        }
        private void DeleteImageFile(string fileName)
        {
            try
            {
                // 1. Get the wwwroot path
                string wwwRootPath = _webHostEnvironment.WebRootPath;

                // 2. Define the full file path
                string filePath = Path.Combine(wwwRootPath, "Admin", "img", fileName);

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