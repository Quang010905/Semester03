using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class EventBookingsController : Controller
    {
        private readonly EventBookingRepository _bookingRepo;
        private readonly EventRepository _eventRepo; // Added to fetch Event details

        public EventBookingsController(EventBookingRepository bookingRepo, EventRepository eventRepo)
        {
            _bookingRepo = bookingRepo;
            _eventRepo = eventRepo;
        }

        // GET: Admin/EventBookings
        public async Task<IActionResult> Index(int? eventId)
        {
            IEnumerable<TblEventBooking> bookings;

            // 1. Load Event List for Dropdown Filter
            var allEvents = await _eventRepo.GetAllAsync();
            ViewData["EventList"] = new SelectList(allEvents, "EventId", "EventName", eventId);

            if (eventId.HasValue)
            {
                // --- FILTERED VIEW (Specific Event) ---
                bookings = await _bookingRepo.GetBookingsForEventAsync(eventId.Value);

                // Get Event Details for Stats
                var selectedEvent = await _eventRepo.GetByIdAdminAsync(eventId.Value);
                if (selectedEvent != null)
                {
                    int maxSlots = selectedEvent.EventMaxSlot;
                    // Use the Repo method to count confirmed slots correctly
                    int soldSlots = await _bookingRepo.GetConfirmedSlotsForEventAsync(eventId.Value);

                    // Calculate Stats
                    ViewData["EventName"] = selectedEvent.EventName;
                    ViewData["MaxSlots"] = maxSlots;
                    ViewData["SoldSlots"] = soldSlots;

                    double occupancy = maxSlots > 0 ? ((double)soldSlots / maxSlots) * 100 : 0;
                    ViewData["Occupancy"] = occupancy.ToString("0.0"); // e.g. "85.5"

                    // Calculate Revenue for this event
                    decimal revenue = bookings.Where(b => b.EventBookingPaymentStatus == 1).Sum(b => b.EventBookingTotalCost ?? 0);
                    ViewData["Revenue"] = revenue;
                }

                ViewData["Title"] = $"Manage: {selectedEvent?.EventName}";
                ViewData["EventFilter"] = eventId.Value;
            }
            else
            {
                // --- DEFAULT VIEW (All Bookings) ---
                bookings = await _bookingRepo.GetAllAsync();
                ViewData["Title"] = "Event Booking Management";

                // Global Stats (Optional)
                ViewData["TotalBookings"] = bookings.Count();
                ViewData["TotalRevenue"] = bookings.Where(b => b.EventBookingPaymentStatus == 1).Sum(b => b.EventBookingTotalCost ?? 0);
            }

            return View(bookings);
        }

        // GET: Admin/EventBookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var booking = await _bookingRepo.GetByIdWithHistoryAsync(id.Value);
            if (booking == null) return NotFound();
            return View(booking);
        }

        // POST: Admin/EventBookings/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, int paymentStatus)
        {
            int adminId = 1;
            var result = await _bookingRepo.UpdateStatusByAdminAsync(id, paymentStatus, adminId);

            if (result) TempData["Success"] = "Booking status updated successfully.";
            else TempData["Error"] = "Could not update booking status.";

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}