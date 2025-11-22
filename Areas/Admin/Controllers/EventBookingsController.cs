using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class EventBookingsController : Controller
    {
        private readonly EventBookingRepository _bookingRepo;

        public EventBookingsController(EventBookingRepository bookingRepo)
        {
            _bookingRepo = bookingRepo;
        }

        // GET: Admin/EventBookings
        public async Task<IActionResult> Index(int? eventId)
        {
            IEnumerable<TblEventBooking> bookings;

            if (eventId.HasValue)
            {
                // If ID is provided, get filtered bookings
                bookings = await _bookingRepo.GetBookingsForEventAsync(eventId.Value);
                ViewData["Title"] = $"Bookings for Event #{eventId.Value}";
                ViewData["EventFilter"] = eventId.Value;
            }
            else
            {
                // If no ID, get all bookings
                bookings = await _bookingRepo.GetAllAsync();
                ViewData["Title"] = "Event Booking Management";
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

        // === ADD NEW ACTION TO UPDATE STATUS ===
        // POST: Admin/EventBookings/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, int paymentStatus)
        {
            int adminId = 1;
            var result = await _bookingRepo.UpdateStatusByAdminAsync(id, paymentStatus, adminId);

            if (result)
            {
                TempData["Success"] = "Booking status updated successfully.";
            }
            else
            {
                TempData["Error"] = "Could not update booking status.";
            }

            // Redirect back to the Details page for that booking
            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}
