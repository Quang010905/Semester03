using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using Semester03.Services.Email;
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
        private readonly IEmailSender _emailSender;

        public EventsController(EventRepository eventRepo, IWebHostEnvironment webHostEnvironment, AbcdmallContext context, IEmailSender emailSender)
        {
            _eventRepo = eventRepo;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
            _emailSender = emailSender;
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
                        if (existingEvt.EventStatus == 1)
                        {
                            // Nếu form bị disable, giá trị gửi lên sẽ là null/default.
                            // Ta phải giữ nguyên giá trị cũ.

                            // 1. Tên (Input text disabled -> null)
                            if (string.IsNullOrEmpty(tblEvent.EventName)) tblEvent.EventName = existingEvt.EventName;

                            // 2. Ngày tháng (Input date disabled -> MinValue)
                            if (tblEvent.EventStart == DateTime.MinValue) tblEvent.EventStart = existingEvt.EventStart;
                            if (tblEvent.EventEnd == DateTime.MinValue) tblEvent.EventEnd = existingEvt.EventEnd;

                            // 3. Vị trí (Đã làm rồi)
                            if (tblEvent.EventTenantPositionId == 0) tblEvent.EventTenantPositionId = existingEvt.EventTenantPositionId;

                            // --- VALIDATION CHẶT CHẼ HƠN ---
                            // Dù Frontend có hack để enable, Backend vẫn phải chặn thay đổi
                            if (tblEvent.EventName != existingEvt.EventName ||
                                tblEvent.EventStart != existingEvt.EventStart ||
                                tblEvent.EventEnd != existingEvt.EventEnd ||
                                tblEvent.EventTenantPositionId != existingEvt.EventTenantPositionId ||
                                    tblEvent.EventUnitPrice != existingEvt.EventUnitPrice)
                            {
                                TempData["Error"] = "Action Denied: Name, Date, and Location cannot be changed for an Active event.";
                                return RedirectToAction(nameof(Index));
                            }
                        }
                        var soldSlots = await _context.TblEventBookings
                            .Where(b => b.EventBookingEventId == tblEvent.EventId
                                        && b.EventBookingStatus == 1
                                        && b.EventBookingPaymentStatus != 3)
                            .SumAsync(b => b.EventBookingQuantity ?? 0);

                        // Nếu số Slot mới < Số vé đã bán -> CHẶN
                        if (tblEvent.EventMaxSlot < soldSlots)
                        {
                            TempData["Error"] = $"Cannot decrease Max Slots to {tblEvent.EventMaxSlot}. You have already sold {soldSlots} tickets.";
                            return RedirectToAction(nameof(Index));
                        }
                        if (existingEvt.EventStatus == 1)
                        {
                            // Nếu ID vị trí gửi lên KHÁC ID vị trí cũ -> CHẶN
                            if (tblEvent.EventTenantPositionId != existingEvt.EventTenantPositionId)
                            {
                                TempData["Error"] = "Action Denied: You cannot change the Location of an Approved/Active event. Please Reject or Cancel it instead.";
                                return RedirectToAction(nameof(Index));
                            }
                        }
                        bool hasBookings = await _context.TblEventBookings
                            .AnyAsync(b => b.EventBookingEventId == tblEvent.EventId && b.EventBookingStatus == 1);

                        if (hasBookings)
                        {
                            // Nếu đã có khách đặt, cấm sửa Ngày Giờ và Địa Điểm
                            // Chỉ cho sửa Tên, Mô tả, Ảnh, Số lượng slot (nếu tăng thêm)
                            if (tblEvent.EventStart != existingEvt.EventStart ||
                                tblEvent.EventEnd != existingEvt.EventEnd ||
                                tblEvent.EventTenantPositionId != existingEvt.EventTenantPositionId ||
                                tblEvent.EventUnitPrice != existingEvt.EventUnitPrice)
                            {
                                TempData["Error"] = "Cannot change Date, Location, or Price because there are active bookings. Please Cancel this event and create a new one if rescheduling is needed.";
                                return RedirectToAction(nameof(Index));
                            }
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
                TempData["Error"] = "Cannot DELETE because bookings exist (Active or History). Please use 'Cancel Event' in Details page to stop the event.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(evt.EventImg)) DeleteImageFile(evt.EventImg);

            await _eventRepo.DeleteAsync(id);
            TempData["Success"] = "Event deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelEvent(int id, string reason)
        {
            // 1. Get Event
            var eventObj = await _context.TblEvents.FindAsync(id);
            if (eventObj == null) return NotFound();

            // 2. Check Event in past
            if (eventObj.EventStart <= DateTime.Now)
            {
                TempData["Error"] = "Cannot cancel event: It has already started or finished.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // 3. Get List Active Booking (Paid/Unpaid/Free)
            // Status: 1=Paid, 0=Unpaid, 2=Free. (3=Cancelled bỏ qua)
            var activeBookings = await _context.TblEventBookings
                .Include(b => b.EventBookingUser)
                .Where(b => b.EventBookingEventId == id && b.EventBookingPaymentStatus != 3)
                .ToListAsync();

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // A. Update Status -> 3 (Cancelled) 
                    eventObj.EventStatus = 3;
                    _context.TblEvents.Update(eventObj);

                    // B. Update Status Booking -> 3 (Cancelled)
                    foreach (var booking in activeBookings)
                    {
                        booking.EventBookingPaymentStatus = 3; // Cancelled
                        booking.EventBookingStatus = 0;

                    }
                    _context.TblEventBookings.UpdateRange(activeBookings);

                    await _context.SaveChangesAsync();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    TempData["Error"] = "System Error: Could not cancel event.";
                    return RedirectToAction(nameof(Details), new { id = id });
                }
            }

            // 4. Send Email
            int mailCount = 0;
            foreach (var booking in activeBookings)
            {
                if (booking.EventBookingUser != null && !string.IsNullOrEmpty(booking.EventBookingUser.UsersEmail))
                {
                    string subject = $"[URGENT] Event Cancelled: {eventObj.EventName}";
                    string body = $@"
                        <h3 style='color:red;'>Event Cancellation Notice</h3>
                        <p>Dear {booking.EventBookingUser.UsersFullName},</p>
                        <p>We regret to inform you that the event <strong>{eventObj.EventName}</strong> (scheduled for {eventObj.EventStart:dd/MM/yyyy}) has been <strong>CANCELLED</strong> by the organizers.</p>
                        
                        <div style='background-color:#ffeeba; padding:15px; margin: 10px 0; border-left: 5px solid #ffc107;'>
                            <strong>Reason:</strong> {reason ?? "Unforeseen circumstances"}
                        </div>

                        <p>Your booking #{booking.EventBookingId} has been cancelled automatically.</p>
                        <p>If you have already paid, <strong>we will process your refund shortly</strong>. Please contact us if you don't receive it within 7 days.</p>
                        <br/>
                        <p>Sincerely,<br/>ABCD Mall Management</p>";

                    try
                    {
                        await _emailSender.SendEmailAsync(booking.EventBookingUser.UsersEmail, subject, body);
                        mailCount++;
                    }
                    catch { /* Continue */ }
                }
            }

            TempData["Success"] = $"Event cancelled. {activeBookings.Count} bookings cancelled. {mailCount} emails sent.";
            return RedirectToAction(nameof(Details), new { id = id });
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

            bool hasBookings = await _context.TblEventBookings
                .AnyAsync(b => b.EventBookingEventId == id && b.EventBookingStatus == 1);

            if (hasBookings)
            {
                return Json(new
                {
                    success = false,
                    message = "Action Denied: Cannot reschedule this event because tickets have already been sold/booked. Please Cancel and create a new one."
                });
            }

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

            string posName = "Unknown Location";
            if (evt.EventTenantPosition != null)
            {
                posName = $"{evt.EventTenantPosition.TenantPositionLocation} ({evt.EventTenantPosition.TenantPositionAreaM2} m2)";
            }

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
                eventImg = evt.EventImg,
                eventStatus = evt.EventStatus,
                positionName = posName
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