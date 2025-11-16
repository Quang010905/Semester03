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
    public class ParkingLevelsController : Controller
    {
        private readonly ParkingLevelRepository _levelRepo;
        private readonly AbcdmallContext _context; // For dependency check

        public ParkingLevelsController(ParkingLevelRepository levelRepo, AbcdmallContext context)
        {
            _levelRepo = levelRepo;
            _context = context;
        }

        // GET: Admin/ParkingLevels
        public async Task<IActionResult> Index()
        {
            var levels = await _levelRepo.GetAllAsync();
            return View(levels);
        }

        // ==========================================================
        // === THE "ALL-IN-ONE" DETAILS PAGE ===
        // ==========================================================
        // GET: Admin/ParkingLevels/Details/5?editSpotId=10
        public async Task<IActionResult> Details(int id, int? editSpotId)
        {
            var level = await _levelRepo.GetByIdWithSpotsAsync(id);
            if (level == null) return NotFound();

            // Prepare the ViewModel
            var viewModel = new ParkingLevelDetailViewModel
            {
                ParkingLevel = level,
                // Prepare an empty 'NewSpot' form, pre-filled
                NewSpot = new TblParkingSpot { SpotLevelId = id, SpotStatus = 0 },
                SpotToEdit = null
            };

            // Check if user clicked an "Edit" button
            if (editSpotId.HasValue)
            {
                viewModel.SpotToEdit = await _context.TblParkingSpots.FindAsync(editSpotId.Value);
            }

            return View(viewModel);
        }

        // GET: Admin/ParkingLevels/Create
        public IActionResult Create()
        {
            var model = new TblParkingLevel { LevelCapacity = 100 };
            return View(model);
        }

        // POST: Admin/ParkingLevels/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("LevelName,LevelCapacity")] TblParkingLevel tblParkingLevel)
        {
            if (tblParkingLevel.LevelCapacity <= 0)
            {
                ModelState.AddModelError("LevelCapacity", "Capacity must be greater than 0.");
            }

            if (ModelState.IsValid)
            {
                await _levelRepo.AddAsync(tblParkingLevel);
                return RedirectToAction(nameof(Index));
            }
            return View(tblParkingLevel);
        }

        // GET: Admin/ParkingLevels/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var level = await _levelRepo.GetByIdAsync(id.Value);
            if (level == null) return NotFound();
            return View(level);
        }

        // POST: Admin/ParkingLevels/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("LevelId,LevelName,LevelCapacity")] TblParkingLevel tblParkingLevel)
        {
            if (id != tblParkingLevel.LevelId) return NotFound();

            if (tblParkingLevel.LevelCapacity <= 0)
            {
                ModelState.AddModelError("LevelCapacity", "Capacity must be greater than 0.");
            }

            // ADVANCED CHECK: Prevent reducing capacity below current defined spots
            var currentSpotCount = await _context.TblParkingSpots.CountAsync(s => s.SpotLevelId == id);
            if (tblParkingLevel.LevelCapacity < currentSpotCount)
            {
                ModelState.AddModelError("LevelCapacity", $"Cannot set capacity to {tblParkingLevel.LevelCapacity}. This level already has {currentSpotCount} spots defined.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _levelRepo.UpdateAsync(tblParkingLevel);
                }
                catch (DbUpdateConcurrencyException)
                {
                    var exists = await _levelRepo.GetByIdAsync(id);
                    if (exists == null) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tblParkingLevel);
        }

        // GET: Admin/ParkingLevels/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var level = await _levelRepo.GetByIdAsync(id.Value);
            if (level == null) return NotFound();

            // Check for dependencies (Tbl_ParkingSpot)
            bool hasSpots = await _context.TblParkingSpots.AnyAsync(s => s.SpotLevelId == id);
            if (hasSpots)
            {
                ViewData["HasDependencies"] = true;
                ViewData["ErrorMessage"] = "This level cannot be deleted. It has existing parking spots linked to it. Please delete those spots first.";
            }

            return View(level);
        }

        // POST: Admin/ParkingLevels/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            bool hasSpots = await _context.TblParkingSpots.AnyAsync(s => s.SpotLevelId == id);
            if (hasSpots)
            {
                TempData["Error"] = "This level cannot be deleted (it has spots).";
                return RedirectToAction(nameof(Index));
            }

            await _levelRepo.DeleteAsync(id);
            TempData["Success"] = "Parking level deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
