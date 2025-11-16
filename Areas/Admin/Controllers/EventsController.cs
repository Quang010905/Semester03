using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class EventsController : Controller
    {
        private readonly EventRepository _eventRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly AbcdmallContext _context; // For dropdowns

        public EventsController(EventRepository eventRepo, IWebHostEnvironment webHostEnvironment, AbcdmallContext context)
        {
            _eventRepo = eventRepo;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }

        // --- Helper method to populate dropdowns ---
        private void PopulatePositionsDropdown(object selectedPosition = null)
        {
            // Get Position list
            ViewData["PositionList"] = new SelectList(
                _context.TblTenantPositions.OrderBy(p => p.TenantPositionLocation),
                "TenantPositionId", "TenantPositionLocation", selectedPosition);
        }

        // --- Helper for saving images (Same as MoviesController) ---
        private async Task<string> SaveImageFileAsync(IFormFile imageFile)
        {
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            // CHANGE: Save to wwwroot/Content/Uploads/Events
            string savePath = Path.Combine(wwwRootPath, "Content", "Uploads", "Events");

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

        // --- Helper for deleting images ---
        private void DeleteImageFile(string fileName)
        {
            try
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                // CHANGE: Delete from wwwroot/Content/Uploads/Events
                string filePath = Path.Combine(wwwRootPath, "Content", "Uploads", "Events", fileName);

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

        // GET: Admin/Events
        public async Task<IActionResult> Index()
        {
            var events = await _eventRepo.GetAllAsync();
            return View(events);
        }

        // GET: Admin/Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var evt = await _eventRepo.GetByIdAdminAsync(id.Value); // Use Admin method
            if (evt == null) return NotFound();
            return View(evt);
        }

        // GET: Admin/Events/Create
        public IActionResult Create()
        {
            PopulatePositionsDropdown();

            // We must round the time to avoid the browser step validation error.
            var now = DateTime.Now;

            // Round down to the current minute (e.g., 4:31:53 -> 4:31:00)
            // And add 1 hour as a sensible default start
            var defaultStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0)
                                .AddHours(1);

            var model = new TblEvent
            {
                EventStatus = 1,
                EventStart = defaultStart, // Use the "clean" value
                EventEnd = defaultStart.AddDays(1) // Use the "clean" value
            };
            return View(model);
        }

        // POST: Admin/Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("EventName,EventDescription,EventStart,EventEnd,EventStatus,EventMaxSlot,EventTenantPositionId")] TblEvent tblEvent,
            IFormFile? imageFile)
        {
            // --- 1. Basic Validation (as before) ---
            ModelState.Remove("EventImg");
            ModelState.Remove("EventTenantPosition");

            if (imageFile == null)
            {
                ModelState.AddModelError("EventImg", "An event image is required.");
            }

            // --- 2. Business Logic Validation ---

            // Rule 1: MaxSlot must be greater than 0
            if (tblEvent.EventMaxSlot <= 0)
            {
                ModelState.AddModelError("EventMaxSlot", "Max Slots must be a positive number (greater than 0).");
            }

            // Get today's date at midnight (to compare)
            var today = DateTime.Now.Date;

            // Rule 2: Start time cannot be in the past
            if (tblEvent.EventStart < today)
            {
                ModelState.AddModelError("EventStart", "Event Start Time cannot be in the past.");
            }

            // Rule 3: End time must be after start time
            if (tblEvent.EventEnd <= tblEvent.EventStart)
            {
                ModelState.AddModelError("EventEnd", "Event End Time must be after the Start Time.");
            }
            // --- End of Business Logic Validation ---


            // 3. Check ModelState (now includes ALL errors)
            if (ModelState.IsValid)
            {
                if (imageFile != null)
                {
                    tblEvent.EventImg = await SaveImageFileAsync(imageFile);
                }

                await _eventRepo.AddAsync(tblEvent);
                return RedirectToAction(nameof(Index));
            }

            // If we are here, something failed (e.g., "Start time in past")
            PopulatePositionsDropdown(tblEvent.EventTenantPositionId);
            return View(tblEvent);
        }

        // GET: Admin/Events/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var evt = await _eventRepo.GetByIdAdminAsync(id.Value); // Use Admin method
            if (evt == null) return NotFound();

            PopulatePositionsDropdown(evt.EventTenantPositionId);
            return View(evt);
        }

        // POST: Admin/Events/Edit/5
        // [POST] Admin/Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("EventId,EventName,EventImg,EventDescription,EventStart,EventEnd,EventStatus,EventMaxSlot,EventTenantPositionId")] TblEvent tblEvent,
            IFormFile? imageFile)
        {
            if (id != tblEvent.EventId) return NotFound();

            // --- 1. Basic Validation ---
            ModelState.Remove("EventTenantPosition");

            // --- 2. Business Logic Validation (Same as Create) ---

            // Rule 1: MaxSlot must be > 0
            if (tblEvent.EventMaxSlot <= 0)
            {
                ModelState.AddModelError("EventMaxSlot", "Max Slots must be a positive number (greater than 0).");
            }

            // Get today's date at midnight (to compare)
            var today = DateTime.Now.Date;

            // Rule 2: Start time cannot be in the past
            // Note: This rule is very strict. If an event has started
            // and you edit it, you might need to adjust this logic.
            // But for now, we follow the requirement.
            if (tblEvent.EventStart < today)
            {
                ModelState.AddModelError("EventStart", "Event Start Time cannot be in the past.");
            }

            // Rule 3: End time must be after start time
            if (tblEvent.EventEnd <= tblEvent.EventStart)
            {
                ModelState.AddModelError("EventEnd", "Event End Time must be after the Start Time.");
            }
            // --- End of Business Logic Validation ---

            // 3. Check ModelState
            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null)
                    {
                        // Delete old image if it exists
                        if (!string.IsNullOrEmpty(tblEvent.EventImg))
                        {
                            DeleteImageFile(tblEvent.EventImg);
                        }
                        tblEvent.EventImg = await SaveImageFileAsync(imageFile);
                    }

                    await _eventRepo.UpdateAsync(tblEvent);
                }
                catch (DbUpdateConcurrencyException)
                {
                    var exists = await _eventRepo.GetByIdAdminAsync(id);
                    if (exists == null) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            // If we are here, something failed
            PopulatePositionsDropdown(tblEvent.EventTenantPositionId);
            return View(tblEvent);
        }

        // GET: Admin/Events/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var evt = await _eventRepo.GetByIdAdminAsync(id.Value);
            if (evt == null) return NotFound();

            // Check for dependencies 
            bool hasBookings = await _context.TblEventBookings.AnyAsync(b => b.EventBookingEventId == id);
            bool hasComplaints = await _context.TblCustomerComplaints.AnyAsync(c => c.CustomerComplaintEventId == id.Value);

            if (hasBookings || hasComplaints)
            {
                ViewData["HasDependencies"] = true;
                string error = "This event cannot be deleted. It is linked to:";
                if (hasBookings) error += " one or more Bookings.";
                if (hasComplaints) error += " one or more Customer Reviews.";
                ViewData["ErrorMessage"] = error;
            }

            return View(evt);
        }

        // POST: Admin/Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            bool hasBookings = await _context.TblEventBookings.AnyAsync(b => b.EventBookingEventId == id);
            bool hasComplaints = await _context.TblCustomerComplaints.AnyAsync(c => c.CustomerComplaintEventId == id);

            if (hasBookings || hasComplaints)
            {
                TempData["Error"] = "This event cannot be deleted (it has dependencies).";
                return RedirectToAction(nameof(Index));
            }

            var evt = await _eventRepo.GetByIdAdminAsync(id);
            if (evt != null && !string.IsNullOrEmpty(evt.EventImg))
            {
                DeleteImageFile(evt.EventImg);
            }

            await _eventRepo.DeleteAsync(id);
            TempData["Success"] = "Event deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            // Status 1 = Active
            var result = await _eventRepo.UpdateStatusAsync(id, 1);
            if (result)
            {
                TempData["Success"] = "Event has been approved and is now active.";
            }
            else
            {
                TempData["Error"] = "Event not found.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            // We assume Status 0 = Inactive/Rejected
            var result = await _eventRepo.UpdateStatusAsync(id, 0);
            if (result)
            {
                TempData["Success"] = "Event has been rejected and is now inactive.";
            }
            else
            {
                TempData["Error"] = "Event not found.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}