using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class TicketsController : Controller
    {
        private readonly TicketRepository _ticketRepo;

        public TicketsController(TicketRepository ticketRepo)
        {
            _ticketRepo = ticketRepo;
        }

        // GET: Admin/Tickets
        public async Task<IActionResult> Index(int? showtimeId)
        {
            IEnumerable<TblTicket> tickets;

            if (showtimeId.HasValue)
            {
                // If ID is provided, get filtered tickets
                tickets = await _ticketRepo.GetTicketsForShowtimeAsync(showtimeId.Value);
                ViewData["Title"] = $"Tickets for Showtime #{showtimeId.Value}";
                ViewData["ShowtimeFilter"] = showtimeId.Value;
            }
            else
            {
                // If no ID, get all tickets
                tickets = await _ticketRepo.GetAllAsync();
                ViewData["Title"] = "Sold Ticket Management";
            }

            return View(tickets);
        }

        // GET: Admin/Tickets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var ticket = await _ticketRepo.GetByIdAsync(id.Value);
            if (ticket == null) return NotFound();
            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _ticketRepo.CancelTicketAsync(id);

            if (result)
            {
                TempData["Success"] = $"Ticket #{id} has been successfully cancelled and the seat is now available.";
            }
            else
            {
                TempData["Error"] = $"Could not cancel Ticket #{id}. It might already be cancelled or an error occurred.";
            }

            // Redirect back to the Details page for that ticket
            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAllTickets(int id)
        {
            // 'id' here is the ShowtimeId
            var result = await _ticketRepo.CancelAllTicketsForShowtimeAsync(id);

            if (result > 0)
            {
                TempData["Success"] = $"Successfully cancelled {result} tickets for this showtime.";
            }
            else if (result == 0)
            {
                TempData["Error"] = "No 'sold' tickets were found to cancel for this showtime.";
            }
            else
            {
                TempData["Error"] = "An error occurred while cancelling tickets.";
            }

            // Redirect back to the Details page for this showtime
            return RedirectToAction("Details", "Showtimes", new { id = id });
        }
    }
}
