using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class EventsController : Controller
    {
        private readonly EventRepository _eventRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly AbcdmallContext _context;

        public EventsController(EventRepository eventRepo, IWebHostEnvironment webHostEnvironment, AbcdmallContext context)
        {
            _eventRepo = eventRepo;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }

        // --- Helper ---
        private void PopulatePositionsDropdown()
        {
            // Get vacant positions + include current ones in logic if needed
            var positions = _context.TblTenantPositions
                .Where(p => p.TenantPositionStatus == 0)
                .OrderBy(p => p.TenantPositionLocation)
                .Select(p => new { Id = p.TenantPositionId, Text = $"{p.TenantPositionLocation} ({p.TenantPositionAreaM2} m2)" })
                .ToList();

            ViewData["PositionList"] = new SelectList(positions, "Id", "Text");
        }

        // GET: Admin/Events (Main Dashboard)
        public async Task<IActionResult> Index()
        {
            var events = await _eventRepo.GetAllAsync();
            PopulatePositionsDropdown();
            return View(events);
        }

        // GET: Admin/Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var evt = await _eventRepo.GetByIdAdminAsync(id.Value);
            if (evt == null) return NotFound();
            return View(evt);
        }

        // ==========================================================
        // === UNIFIED SAVE (Create & Edit) ===
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveEvent(
            [Bind("EventId,EventName,EventDescription,EventStart,EventEnd,EventStatus,EventMaxSlot,EventTenantPositionId,EventUnitPrice")] TblEvent tblEvent,
            IFormFile? imageFile)
        {
            ModelState.Remove("EventTenantPosition");
            ModelState.Remove("EventImg");

            // Basic Validations
            if (tblEvent.EventMaxSlot <= 0) ModelState.AddModelError("EventMaxSlot", "Max Slots must be > 0.");
            if (tblEvent.EventEnd <= tblEvent.EventStart) ModelState.AddModelError("EventEnd", "End Time must be after Start Time.");
            
            bool isConflict = await _eventRepo.CheckOverlapAsync(
                tblEvent.EventTenantPositionId,
                tblEvent.EventStart,
                tblEvent.EventEnd,
                tblEvent.EventId == 0 ? null : tblEvent.EventId // Exclude ID if Edit
            );

            if (isConflict)
            {
                var posName = _context.TblTenantPositions
                    .Where(p => p.TenantPositionId == tblEvent.EventTenantPositionId)
                    .Select(p => p.TenantPositionLocation)
                    .FirstOrDefault();

                ModelState.AddModelError("EventTenantPositionId", $"Location '{posName}' is already booked for this time range.");
                TempData["Error"] = "Booking Conflict! Please choose another time or location.";
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    string newFileName = null;
                    if (imageFile != null) newFileName = await SaveImageFileAsync(imageFile);

                    // CASE 1: CREATE
                    if (tblEvent.EventId == 0)
                    {
                        if (tblEvent.EventStart < DateTime.Now)
                        {
                            TempData["Error"] = "Cannot create an event in the past.";
                            return RedirectToAction(nameof(Index));
                        }

                        if (newFileName == null)
                        {
                            TempData["Error"] = "Image is required for new events.";
                            return RedirectToAction(nameof(Index));
                        }

                        tblEvent.EventImg = newFileName;
                        await _eventRepo.AddAsync(tblEvent);
                        TempData["Success"] = "Event created successfully.";
                    }
                    // CASE 2: EDIT
                    else
                    {
                        var existingEvt = await _eventRepo.GetByIdAdminAsync(tblEvent.EventId);

                        // === STRICT RULE: Cannot edit if started ===
                        if (existingEvt.EventStart <= DateTime.Now)
                        {
                            TempData["Error"] = "Action Denied: This event has already started/finished.";
                            return RedirectToAction(nameof(Index));
                        }

                        if (newFileName != null)
                        {
                            if (!string.IsNullOrEmpty(existingEvt.EventImg)) DeleteImageFile(existingEvt.EventImg);
                            tblEvent.EventImg = newFileName;
                        }
                        else
                        {
                            tblEvent.EventImg = existingEvt.EventImg;
                        }

                        await _eventRepo.UpdateAsync(tblEvent);
                        TempData["Success"] = "Event updated successfully.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error: " + ex.Message;
                }
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Validation failed. Please check inputs.";
            return RedirectToAction(nameof(Index));
        }

        // [POST] Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var evt = await _eventRepo.GetByIdAdminAsync(id);
            if (evt == null) return NotFound();

            // === STRICT RULE: Cannot delete if started ===
            if (evt.EventStart <= DateTime.Now)
            {
                TempData["Error"] = "Action Denied: Cannot delete ongoing/past events.";
                return RedirectToAction(nameof(Index));
            }

            bool hasBookings = await _context.TblEventBookings.AnyAsync(b => b.EventBookingEventId == id);
            if (hasBookings)
            {
                TempData["Error"] = "Cannot delete: Event has bookings.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(evt.EventImg)) DeleteImageFile(evt.EventImg);

            await _eventRepo.DeleteAsync(id);
            TempData["Success"] = "Event deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // [POST] Reschedule (Drag & Drop)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RescheduleEvent(int id, DateTime newStart, DateTime newEnd)
        {
            var evt = await _eventRepo.GetByIdAdminAsync(id);
            if (evt == null) return Json(new { success = false, message = "Not found" });

            if (evt.EventStart <= DateTime.Now)
                return Json(new { success = false, message = "Cannot move started events." });

            if (newStart < DateTime.Now)
                return Json(new { success = false, message = "Cannot move to past." });

            bool isConflict = await _eventRepo.CheckOverlapAsync(
                evt.EventTenantPositionId,
                newStart,
                newEnd,
                id // <--- Exclude ID
            );

            if (isConflict)
            {
                return Json(new { success = false, message = "Time Conflict! Another event is already scheduled here." });
            }

            evt.EventStart = newStart;
            evt.EventEnd = newEnd;
            await _eventRepo.UpdateAsync(evt);
            return Json(new { success = true });
        }

        // [POST] Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var evt = await _eventRepo.GetByIdAdminAsync(id);
            if (evt != null && evt.EventEnd > DateTime.Now) // Allow ongoing, Block ended
            {
                await _eventRepo.UpdateStatusAsync(id, 1);
                TempData["Success"] = "Event approved.";
            }
            else
            {
                TempData["Error"] = "Cannot approve ended events.";
            }
            return RedirectToAction(nameof(Index));
        }

        // [POST] Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            await _eventRepo.UpdateStatusAsync(id, 0);
            TempData["Success"] = "Event rejected.";
            return RedirectToAction(nameof(Index));
        }

        // --- API for Calendar & Modal ---
        [HttpGet]
        public async Task<IActionResult> GetCalendarData()
        {
            var data = await _eventRepo.GetCalendarEventsAsync();
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetEventJson(int id)
        {
            var evt = await _eventRepo.GetByIdAdminAsync(id);
            if (evt == null) return NotFound();

            return Json(new
            {
                id = evt.EventId,
                eventName = evt.EventName,
                eventDescription = evt.EventDescription,
                eventStart = evt.EventStart.ToString("yyyy-MM-ddTHH:mm"),
                eventEnd = evt.EventEnd.ToString("yyyy-MM-ddTHH:mm"),
                eventMaxSlot = evt.EventMaxSlot,
                eventUnitPrice = evt.EventUnitPrice,
                eventTenantPositionId = evt.EventTenantPositionId,
                eventImg = evt.EventImg
            });
        }

        // --- File Helpers ---
        private async Task<string> SaveImageFileAsync(IFormFile imageFile)
        {
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            string savePath = Path.Combine(wwwRootPath, "Content", "Uploads", "Events");
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            using (var fileStream = new FileStream(Path.Combine(savePath, fileName), FileMode.Create)) { await imageFile.CopyToAsync(fileStream); }
            return fileName;
        }
        private void DeleteImageFile(string fileName)
        {
            try
            {
                string path = Path.Combine(_webHostEnvironment.WebRootPath, "Content", "Uploads", "Events", fileName);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch { }
        }
    }
}