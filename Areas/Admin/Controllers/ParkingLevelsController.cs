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
        public async Task<IActionResult> Edit(int? id, string? returnUrl)
        {
            if (id == null) return NotFound();
            var level = await _levelRepo.GetByIdAsync(id.Value);
            if (level == null) return NotFound();
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Url.Action(nameof(Index));
            }
            ViewData["ReturnUrl"] = returnUrl;

            return View(level);
        }

        // POST: Admin/ParkingLevels/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("LevelId,LevelName,LevelCapacity")] TblParkingLevel tblParkingLevel, string? returnUrl)
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
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
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

            int totalSpots = await _context.TblParkingSpots.CountAsync(s => s.SpotLevelId == id);
            int occupiedSpots = await _context.TblParkingSpots.CountAsync(s => s.SpotLevelId == id && s.SpotStatus == 1);

            ViewData["TotalSpots"] = totalSpots;
            ViewData["OccupiedSpots"] = occupiedSpots;

            if (occupiedSpots > 0)
            {
                ViewData["ErrorMessage"] = $"STOP! This level has {occupiedSpots} cars currently parked. You CANNOT delete it until they leave.";
                ViewData["CanDelete"] = false;
            }
            else if (totalSpots > 0)
            {
                ViewData["ErrorMessage"] = $"WARNING: This level has {totalSpots} configured spots (Empty). Deleting the level will delete ALL these spots configuration.";
                ViewData["CanDelete"] = true;
            }
            else
            {
                ViewData["CanDelete"] = true;
            }

            return View(level);
        }

        // POST: Admin/ParkingLevels/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var level = await _levelRepo.GetByIdAsync(id);
            if (level == null) return NotFound();

            var spots = await _context.TblParkingSpots.Where(s => s.SpotLevelId == id).ToListAsync();

            // --- (REALITY CHECK) ---

            
            if (spots.Any(s => s.SpotStatus == 1))
            {
                TempData["Error"] = "CRITICAL: Cannot delete this level because there are cars currently parked (Occupied spots)! Please clear the parking lot first.";
                return RedirectToAction(nameof(Index));
            }

            
            if (spots.Any())
            {
                _context.TblParkingSpots.RemoveRange(spots);
            }

            
            await _levelRepo.DeleteAsync(id);

             

            TempData["Success"] = "Parking level and all associated empty spots were deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
