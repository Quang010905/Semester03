using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class CinemaSettingsController : Controller
    {
        private readonly CinemaRepository _cinemaRepo;
        // 1. ADD IWebHostEnvironment
        private readonly IWebHostEnvironment _webHostEnvironment;

        // 2. MODIFY Constructor to inject IWebHostEnvironment
        public CinemaSettingsController(CinemaRepository cinemaRepo, IWebHostEnvironment webHostEnvironment)
        {
            _cinemaRepo = cinemaRepo;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/CinemaSettings
        public async Task<IActionResult> Index()
        {
            var cinemaSettings = await _cinemaRepo.GetSettingsAsync();
            return View(cinemaSettings);
        }

        // 3. MODIFY [POST] Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            // Bind all fields from the form
            [Bind("CinemaId,CinemaName,CinemaImg,CinemaDescription")] TblCinema tblCinema,
            IFormFile? imageFile) // Add the file parameter
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if a new file was uploaded
                    if (imageFile != null)
                    {
                        // Delete the old image file, if it exists
                        if (!string.IsNullOrEmpty(tblCinema.CinemaImg))
                        {
                            DeleteImageFile(tblCinema.CinemaImg);
                        }
                        // Save the new file and update the model
                        tblCinema.CinemaImg = await SaveImageFileAsync(imageFile);
                    }
                    // If imageFile is null, tblCinema.CinemaImg (from the hidden field)
                    // will keep its old value, and no file changes are made.

                    await _cinemaRepo.UpdateSettingsAsync(tblCinema);
                    TempData["Success"] = "Cinema settings saved successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    TempData["Error"] = "An error occurred. Please try again.";
                }
                return RedirectToAction(nameof(Index));
            }
            // If model is invalid
            return View(tblCinema);
        }


        // 4. ADD Helper Methods for saving/deleting files

        private async Task<string> SaveImageFileAsync(IFormFile imageFile)
        {
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            // We'll save Cinema images to their own folder
            string savePath = Path.Combine(wwwRootPath, "Content", "Uploads", "Cinema");

            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            string filePath = Path.Combine(savePath, fileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }
            return fileName; // Return only the filename
        }

        private void DeleteImageFile(string fileName)
        {
            try
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string filePath = Path.Combine(wwwRootPath, "Content", "Uploads", "Cinema", fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {fileName}. Error: {ex.Message}");
            }
        }
    }
}
