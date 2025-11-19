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
    public class SeatsController : Controller
    {
        private readonly SeatRepository _seatRepo;
        // 1. ADD THE CONTEXT (needed for helper methods)
        private readonly AbcdmallContext _context;

        // 2. MODIFY CONSTRUCTOR
        public SeatsController(SeatRepository seatRepo, AbcdmallContext context)
        {
            _seatRepo = seatRepo;
            _context = context;
        }

        // Note: [GET] Index, Details, Create, Edit are REMOVED 
        // because management is now on the Screen/Details page.

        // [POST] Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("SeatId,SeatScreenId,SeatLabel,SeatRow,SeatCol,SeatIsActive")] TblSeat tblSeat)
        {
            // Remove validation for the navigation property
            ModelState.Remove("SeatScreen");

            // Collision Check
            bool collision = await _seatRepo.CheckCollisionAsync(tblSeat.SeatScreenId, tblSeat.SeatRow, tblSeat.SeatCol, tblSeat.SeatId);
            if (collision)
            {
                TempData["Error"] = $"A seat at Row {tblSeat.SeatRow}, Column {tblSeat.SeatCol} already exists.";
                return RedirectToAction("Details", "Screens", new { id = tblSeat.SeatScreenId });
            }

            // Check capacity limit
            var screen = await GetScreenWithSeatCountAsync(tblSeat.SeatScreenId);
            if (screen != null && screen.TblSeats.Count >= screen.ScreenSeats) //
            {
                TempData["Error"] = $"Cannot add seat. This screen is limited to {screen.ScreenSeats} seats.";
                return RedirectToAction("Details", "Screens", new { id = tblSeat.SeatScreenId });
            }

            if (ModelState.IsValid)
            {
                tblSeat.SeatId = 0;
                await _seatRepo.AddAsync(tblSeat);
                TempData["Success"] = $"Seat {tblSeat.SeatLabel} created successfully.";
                return RedirectToAction("Details", "Screens", new { id = tblSeat.SeatScreenId });
            }

            TempData["Error"] = "Failed to create seat. Please check all fields.";
            return RedirectToAction("Details", "Screens", new { id = tblSeat.SeatScreenId });
        }

        // [POST] Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("SeatId,SeatScreenId,SeatLabel,SeatRow,SeatCol,SeatIsActive")] TblSeat tblSeat)
        {
            ModelState.Remove("SeatScreen");
            if (id != tblSeat.SeatId) return NotFound();

            // Collision Check
            bool collision = await _seatRepo.CheckCollisionAsync(tblSeat.SeatScreenId, tblSeat.SeatRow, tblSeat.SeatCol, tblSeat.SeatId);
            if (collision)
            {
                TempData["Error"] = $"A seat at Row {tblSeat.SeatRow}, Column {tblSeat.SeatCol} already exists.";
                return RedirectToAction("Details", "Screens", new { id = tblSeat.SeatScreenId, editSeatId = id });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _seatRepo.UpdateAsync(tblSeat);
                    TempData["Success"] = $"Seat {tblSeat.SeatLabel} updated successfully.";
                    return RedirectToAction("Details", "Screens", new { id = tblSeat.SeatScreenId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    TempData["Error"] = "An error occurred while saving. Please try again.";
                }
            }

            TempData["Error"] = "Failed to update seat. Please check all fields.";
            return RedirectToAction("Details", "Screens", new { id = tblSeat.SeatScreenId, editSeatId = id });
        }

        // ==========================================================
        // === 3. ADD THE MISSING BATCH CREATE ACTION ===
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchCreate(BatchCreateSeatVm vm)
        {
            if (vm.NumRows <= 0 || vm.NumCols <= 0)
            {
                TempData["Error"] = "Number of rows and columns must be greater than 0.";
                return RedirectToAction("Details", "Screens", new { id = vm.ScreenId });
            }

            // Use the helper method
            var screen = await GetScreenWithSeatCountAsync(vm.ScreenId);

            if (screen == null)
            {
                TempData["Error"] = "Screen not found.";
                return RedirectToAction("Index", "Screens");
            }

            int newSeatCount = vm.NumRows * vm.NumCols;
            int currentSeatCount = screen.TblSeats.Count;
            int limit = screen.ScreenSeats; //

            // Check 1: Don't run batch if seats already exist
            if (currentSeatCount > 0)
            {
                TempData["Error"] = "Cannot batch-generate. Seats already exist for this screen. Use 'Add Individual Seat' for modifications.";
                return RedirectToAction("Details", "Screens", new { id = vm.ScreenId });
            }

            // Check 2: Don't allow creating more seats than the limit
            if (newSeatCount > limit)
            {
                TempData["Error"] = $"Cannot add {newSeatCount} seats. This screen is limited to {limit} seats. Please adjust the 'Total Seats' value for the Screen first.";
                return RedirectToAction("Details", "Screens", new { id = vm.ScreenId });
            }

            var newSeats = new List<TblSeat>();

            for (int r = 0; r < vm.NumRows; r++)
            {
                string rowLabel = ((char)(65 + r)).ToString();
                for (int c = 1; c <= vm.NumCols; c++)
                {
                    newSeats.Add(new TblSeat
                    {
                        SeatScreenId = vm.ScreenId,
                        SeatLabel = $"{rowLabel}{c}",
                        SeatRow = rowLabel,
                        SeatCol = c,
                        SeatIsActive = true
                    });
                }
            }

            // Make sure your repo has this method!
            await _seatRepo.BatchAddAsync(newSeats);
            TempData["Success"] = $"Successfully generated {newSeats.Count} seats.";
            return RedirectToAction("Details", "Screens", new { id = vm.ScreenId });
        }

        

        // ==========================================================
        // === 5. ADD THE HELPER METHODS ===
        // ==========================================================
        private async Task<TblScreen> GetScreenWithSeatCountAsync(int screenId)
        {
            // This method gets the Screen and its related seats
            return await _context.TblScreens
                                 .Include(s => s.TblSeats)
                                 .FirstOrDefaultAsync(s => s.ScreenId == screenId);
        }

        
    }
}