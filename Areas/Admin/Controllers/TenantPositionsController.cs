// ... (Usings unchanged)

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
    public class TenantPositionsController : Controller
    {
        private readonly TenantPositionRepository _posRepo;
        private readonly AbcdmallContext _context;

        public TenantPositionsController(TenantPositionRepository posRepo, AbcdmallContext context)
        {
            _posRepo = posRepo;
            _context = context;
        }

        // --- HELPER: Populate BOTH Dropdowns ---
        private void PopulateDropdowns(object selectedTenant = null, object selectedCinema = null)
        {
            var tenants = _context.TblTenants.Where(t => t.TenantStatus == 1).OrderBy(t => t.TenantName);
            ViewData["TenantList"] = new SelectList(tenants, "TenantId", "TenantName", selectedTenant);

        }

        // GET: Admin/TenantPositions
        public async Task<IActionResult> Index(string search, int? floor, int? status)
        {
            // Call the updated repository method
            var positions = await _posRepo.GetAllAdminEntitiesAsync(search, floor, status);

            // Group by Floor for the map view
            var groupedByFloor = positions
                .GroupBy(p => p.TenantPositionFloor)
                .OrderBy(g => g.Key);

            // Pass search values back to View (to keep form filled)
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentFloor"] = floor;
            ViewData["CurrentStatus"] = status;

            return View(groupedByFloor);
        }


        // ==========================================================
        // === DETAILS (Read-Only) ===
        // ==========================================================
        // GET: Admin/TenantPositions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Use the Admin method to get full details including Tenant name
            var position = await _posRepo.GetEntityByIdAsync(id.Value);

            if (position == null) return NotFound();


            return View(position);
        }

        // ==========================================================
        // === EDIT (Assign Tenant) ===
        // ==========================================================
        // GET: Admin/TenantPositions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var position = await _posRepo.GetEntityByIdAsync(id.Value);
            if (position == null) return NotFound();
            
            if (position.TenantPositionAssignedCinemaId != null)
            {
                TempData["Error"] = "This is a fixed Cinema unit and cannot be edited.";
                return RedirectToAction(nameof(Index));
            }
            
            PopulateDropdowns(position.TenantPositionAssignedTenantId);
            return View(position);
        }

        // POST: Admin/TenantPositions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TblTenantPosition tblPosition)
        {
            if (id != tblPosition.TenantPositionId) return NotFound();

            tblPosition.TenantPositionAssignedCinemaId = null;

            // We don't validate the navigation property
            ModelState.Remove("TenantPositionAssignedTenant");
            ModelState.Remove("TenantPositionAssignedCinema");

            if (ModelState.IsValid)
            {
                try
                {
                    if (tblPosition.TenantPositionAssignedTenantId != null)
                    {
                        tblPosition.TenantPositionStatus = 1; // Occupied
                    }
                    else
                    {
                        tblPosition.TenantPositionStatus = 0; // Vacant
                    }

                    await _posRepo.UpdateAsync(tblPosition);
                    TempData["Success"] = $"Unit {tblPosition.TenantPositionLocation} updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (await _posRepo.GetEntityByIdAsync(id) == null) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns(tblPosition.TenantPositionAssignedTenantId, tblPosition.TenantPositionAssignedCinemaId);
            return View(tblPosition);
        }
    }

}