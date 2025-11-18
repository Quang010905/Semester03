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

            // Populate Dropdown: Show "Active" tenants
            var tenants = _context.TblTenants
                .Where(t => t.TenantStatus == 1)
                .OrderBy(t => t.TenantName);

            ViewData["TenantList"] = new SelectList(tenants, "TenantId", "TenantName", position.TenantPositionAssignedTenantId);

            return View(position);
        }

        // POST: Admin/TenantPositions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TblTenantPosition tblPosition)
        {
            if (id != tblPosition.TenantPositionId) return NotFound();

            // We don't validate the navigation property
            ModelState.Remove("TenantPositionAssignedTenant");

            if (ModelState.IsValid)
            {
                try
                {
                    // === BUSINESS LOGIC: AUTO STATUS ===
                    // If a tenant is assigned, set status to 1 (Occupied).
                    // If no tenant (null), set status to 0 (Vacant).
                    if (tblPosition.TenantPositionAssignedTenantId != null)
                    {
                        tblPosition.TenantPositionStatus = 1;
                    }
                    else
                    {
                        tblPosition.TenantPositionStatus = 0;
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

            // Reload dropdown if validation fails
            var tenants = _context.TblTenants.Where(t => t.TenantStatus == 1).OrderBy(t => t.TenantName);
            ViewData["TenantList"] = new SelectList(tenants, "TenantId", "TenantName", tblPosition.TenantPositionAssignedTenantId);
            return View(tblPosition);
        }
    }

}