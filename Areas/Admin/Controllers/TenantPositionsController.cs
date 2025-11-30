// ... (Usings unchanged)

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using Semester03.Services.Email;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class TenantPositionsController : Controller
    {
        private readonly TenantPositionRepository _posRepo;
        private readonly AbcdmallContext _context;
        private readonly IEmailSender _emailSender;

        public TenantPositionsController(TenantPositionRepository posRepo, AbcdmallContext context, IEmailSender emailSender)
        {
            _posRepo = posRepo;
            _context = context;
            _emailSender = emailSender;
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
            var positions = await _posRepo.GetAllAdminEntitiesAsync(null, floor, null);

            var groupedByFloor = positions
                .GroupBy(p => p.TenantPositionFloor)
                .OrderBy(g => g.Key);

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

            if (tblPosition.TenantPositionAssignedTenantId != null)
            {
                tblPosition.TenantPositionStatus = 1; // Occupied
                if (tblPosition.PositionLeaseStart == null)
                {
                    ModelState.AddModelError("PositionLeaseStart", "Start Date is required when assigning a tenant.");
                }
                if (tblPosition.PositionLeaseEnd == null)
                {
                    ModelState.AddModelError("PositionLeaseEnd", "End Date is required when assigning a tenant.");
                }
                if (tblPosition.PositionLeaseStart != null && tblPosition.PositionLeaseEnd != null)
                {
                    if (tblPosition.PositionLeaseEnd < tblPosition.PositionLeaseStart)
                    {
                        ModelState.AddModelError("PositionLeaseEnd", "End Date must be AFTER Start Date.");
                    }
                }
            }
            else
            {
                tblPosition.TenantPositionStatus = 0; // Vacant
                tblPosition.PositionLeaseStart = null;
                tblPosition.PositionLeaseEnd = null;
            }

            if (ModelState.IsValid)
            {
                try
                {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Thêm tham số id (nullable)
        public async Task<IActionResult> SendReminders(int? id)
        {
            List<TblTenantPosition> targets = new List<TblTenantPosition>();

            if (id.HasValue)
            {
                // TRƯỜNG HỢP 1: Gửi lẻ cho 1 vị trí cụ thể
                var position = await _posRepo.GetEntityByIdAsync(id.Value);
                // Kiểm tra logic: Phải có tenant, có ngày hết hạn và chưa hết hạn quá lâu (hoặc tùy logic bạn)
                if (position != null && position.TenantPositionAssignedTenantId != null && position.PositionLeaseEnd.HasValue)
                {
                    targets.Add(position);
                }
            }
            else
            {
                // TRƯỜNG HỢP 2: Gửi hàng loạt (như cũ)
                targets = await _posRepo.GetExpiringLeasesWithContactAsync(30);
            }

            int sentCount = 0;
            foreach (var unit in targets)
            {
                var tenantName = unit.TenantPositionAssignedTenant?.TenantName;
                // Navigation property tới Users để lấy Email
                var email = unit.TenantPositionAssignedTenant?.TenantUser?.UsersEmail;

                if (!string.IsNullOrEmpty(email))
                {
                    string subject = $"[ABCD Mall] Lease Expiration Reminder - Unit {unit.TenantPositionLocation}";
                    string body = $@"<h3>Dear {tenantName},</h3>
                            <p>This is a reminder that your lease for unit <strong>{unit.TenantPositionLocation}</strong> 
                            is set to expire on <strong>{unit.PositionLeaseEnd?.ToString("dd/MM/yyyy")}</strong>.</p>
                            <p>Please contact us to renew.</p>";
                    try
                    {
                        await _emailSender.SendEmailAsync(email, subject, body);
                        sentCount++;
                    }
                    catch { /* Log error */ }
                }
            }

            if (sentCount > 0)
                TempData["Success"] = $"Sent reminders to {sentCount} tenant(s).";
            else
                TempData["Info"] = "No eligible tenants found or no emails sent.";

            // Nếu gửi lẻ từ trang Edit, quay lại trang Edit. Nếu gửi loạt, quay lại Index.
            if (id.HasValue) return RedirectToAction(nameof(Edit), new { id = id.Value });
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Evict(int id)
        {
            var position = await _posRepo.GetEntityByIdAsync(id);
            if (position == null) return NotFound();

            var oldTenantName = position.TenantPositionAssignedTenant?.TenantName;

            position.TenantPositionAssignedTenantId = null;
            position.TenantPositionStatus = 0; // Trở về Vacant
            position.PositionLeaseStart = null;
            position.PositionLeaseEnd = null;

            await _posRepo.UpdateAsync(position);

            TempData["Success"] = $"Evicted tenant '{oldTenantName}' from {position.TenantPositionLocation}. Unit is now VACANT.";

            return RedirectToAction(nameof(Edit), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendOverstayNotice(int id)
        {
            var unit = await _posRepo.GetEntityByIdAsync(id);
            if (unit == null || unit.TenantPositionAssignedTenant == null) return NotFound();

            var email = unit.TenantPositionAssignedTenant.TenantUser?.UsersEmail;

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Tenant user does not have an email address.";
                return RedirectToAction(nameof(Edit), new { id = id });
            }

            string subject = $"[URGENT] Lease Expired - Unit {unit.TenantPositionLocation} - Immediate Action Required";
            string body = $@"
                <h3 style='color:red;'>FINAL NOTICE: LEASE EXPIRED</h3>
                <p>Dear {unit.TenantPositionAssignedTenant.TenantName},</p>
                <p>Your lease for unit <strong>{unit.TenantPositionLocation}</strong> expired on <strong>{unit.PositionLeaseEnd?.ToString("dd/MM/yyyy")}</strong>.</p>
                <p>You are currently holding over the premises without a valid contract.</p>
                <p><strong>Please renew your lease or vacate the premises immediately to avoid penalties and legal action.</strong></p>
                <br/>
                <p>ABCD Mall Management</p>";

            try
            {
                await _emailSender.SendEmailAsync(email, subject, body);
                TempData["Success"] = "Overstay/Penalty notice sent successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to send email: " + ex.Message;
            }

            return RedirectToAction(nameof(Edit), new { id = id });
        }
    }

}