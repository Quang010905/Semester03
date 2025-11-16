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
    public class ParkingSpotsController : Controller
    {
        private readonly ParkingSpotRepository _spotRepo;
        private readonly AbcdmallContext _context;

        public ParkingSpotsController(ParkingSpotRepository spotRepo, AbcdmallContext context)
        {
            _spotRepo = spotRepo;
            _context = context;
        }

        // Helper method to check capacity
        private async Task<TblParkingLevel> GetLevelWithSpotCountAsync(int levelId)
        {
            return await _context.TblParkingLevels
                                 .Include(l => l.TblParkingSpots)
                                 .FirstOrDefaultAsync(l => l.LevelId == levelId);
        }

        // [POST] Create (from All-in-One form)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("SpotLevelId,SpotCode,SpotRow,SpotCol,SpotStatus")] TblParkingSpot tblParkingSpot)
        {
            // Remove validation for the navigation property
            ModelState.Remove("SpotLevel");

            // Collision Check
            bool collision = await _spotRepo.CheckCollisionAsync(tblParkingSpot.SpotLevelId, tblParkingSpot.SpotRow, tblParkingSpot.SpotCol);
            if (collision)
            {
                TempData["Error"] = $"A spot at Row {tblParkingSpot.SpotRow}, Column {tblParkingSpot.SpotCol} already exists.";
                return RedirectToAction("Details", "ParkingLevels", new { id = tblParkingSpot.SpotLevelId });
            }

            // Check capacity limit
            var level = await GetLevelWithSpotCountAsync(tblParkingSpot.SpotLevelId);
            if (level != null && level.TblParkingSpots.Count >= level.LevelCapacity)
            {
                TempData["Error"] = $"Cannot add spot. This level is limited to {level.LevelCapacity} spots.";
                return RedirectToAction("Details", "ParkingLevels", new { id = tblParkingSpot.SpotLevelId });
            }

            if (ModelState.IsValid)
            {
                tblParkingSpot.ParkingSpotId = 0;
                await _spotRepo.AddAsync(tblParkingSpot);
                TempData["Success"] = $"Spot {tblParkingSpot.SpotCode} created successfully.";
                return RedirectToAction("Details", "ParkingLevels", new { id = tblParkingSpot.SpotLevelId });
            }

            TempData["Error"] = "Failed to create spot. Please check all fields.";
            return RedirectToAction("Details", "ParkingLevels", new { id = tblParkingSpot.SpotLevelId });
        }

        // [POST] Edit (from All-in-One form)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("ParkingSpotId,SpotLevelId,SpotCode,SpotRow,SpotCol,SpotStatus")] TblParkingSpot tblParkingSpot)
        {
            ModelState.Remove("SpotLevel");
            if (id != tblParkingSpot.ParkingSpotId) return NotFound();

            // Collision Check
            bool collision = await _spotRepo.CheckCollisionAsync(tblParkingSpot.SpotLevelId, tblParkingSpot.SpotRow, tblParkingSpot.SpotCol, tblParkingSpot.ParkingSpotId);
            if (collision)
            {
                TempData["Error"] = $"A spot at Row {tblParkingSpot.SpotRow}, Column {tblParkingSpot.SpotCol} already exists.";
                return RedirectToAction("Details", "ParkingLevels", new { id = tblParkingSpot.SpotLevelId, editSpotId = id });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _spotRepo.UpdateAsync(tblParkingSpot);
                    TempData["Success"] = $"Spot {tblParkingSpot.SpotCode} updated successfully.";
                    return RedirectToAction("Details", "ParkingLevels", new { id = tblParkingSpot.SpotLevelId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    TempData["Error"] = "An error occurred while saving. Please try again.";
                }
            }

            TempData["Error"] = "Failed to update spot. Please check all fields.";
            return RedirectToAction("Details", "ParkingLevels", new { id = tblParkingSpot.SpotLevelId, editSpotId = id });
        }

        // [POST] BatchCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchCreate(BatchCreateSpotVm vm)
        {
            if (vm.NumRows <= 0 || vm.NumCols <= 0)
            {
                TempData["Error"] = "Number of rows and columns must be greater than 0.";
                return RedirectToAction("Details", "ParkingLevels", new { id = vm.LevelId });
            }

            var level = await GetLevelWithSpotCountAsync(vm.LevelId);
            if (level == null) return NotFound();

            int newSeatCount = vm.NumRows * vm.NumCols;
            int currentSeatCount = level.TblParkingSpots.Count;
            int limit = level.LevelCapacity;

            if (currentSeatCount > 0)
            {
                TempData["Error"] = "Cannot batch-generate. Spots already exist for this level.";
                return RedirectToAction("Details", "ParkingLevels", new { id = vm.LevelId });
            }
            if (newSeatCount > limit)
            {
                TempData["Error"] = $"Cannot add {newSeatCount} spots. This level is limited to {limit} spots.";
                return RedirectToAction("Details", "ParkingLevels", new { id = vm.LevelId });
            }

            var newSpots = new List<TblParkingSpot>();
            for (int r = 0; r < vm.NumRows; r++)
            {
                string rowLabel = ((char)(65 + r)).ToString();
                for (int c = 1; c <= vm.NumCols; c++)
                {
                    newSpots.Add(new TblParkingSpot
                    {
                        SpotLevelId = vm.LevelId,
                        SpotCode = $"{rowLabel}{c:D2}", // A01, A02...
                        SpotRow = rowLabel,
                        SpotCol = c,
                        SpotStatus = 0 // 0 = Available
                    });
                }
            }

            await _spotRepo.BatchAddAsync(newSpots);
            TempData["Success"] = $"Successfully generated {newSpots.Count} spots.";
            return RedirectToAction("Details", "ParkingLevels", new { id = vm.LevelId });
        }

        // GET: Admin/ParkingSpots/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var spot = await _spotRepo.GetByIdAsync(id.Value);
            if (spot == null) return NotFound();

            // NOTE: Parking spots have no dependencies in this DB,
            // but in a real system, you might check Tbl_ParkingTickets
            ViewData["HasDependencies"] = false;

            return View(spot);
        }

        // POST: Admin/ParkingSpots/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var spot = await _spotRepo.GetByIdAsync(id);
            if (spot == null) return NotFound();

            int levelId = spot.SpotLevelId;
            await _spotRepo.DeleteAsync(id);
            TempData["Success"] = $"Spot {spot.SpotCode} has been deleted.";
            return RedirectToAction("Details", "ParkingLevels", new { id = levelId });
        }
    }
}
