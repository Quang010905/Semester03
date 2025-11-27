using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using System.Text;

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
        public async Task<IActionResult> Index(string search, DateTime? date, int? showtimeId)
        {
            var tickets = await _ticketRepo.SearchTicketsAsync(search, showtimeId, date);

            ViewData["CurrentSearch"] = search;
            ViewData["CurrentDate"] = date?.ToString("yyyy-MM-dd");
            ViewData["CurrentShowtime"] = showtimeId;

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

        [HttpPost]
        public async Task<IActionResult> ExportSummary(string search, DateTime? date, int? showtimeId)
        {
            // 1. Get Data (Reuse existing search logic to respect filters)
            var tickets = await _ticketRepo.SearchTicketsAsync(search, showtimeId, date);

            // 2. Process Data: Filter 'Sold' -> Group by Movie & Showtime
            var summaryData = tickets
                .Where(t => t.TicketStatus.ToLower() == "sold")
                .GroupBy(t => new
                {
                    Movie = t.TicketShowtimeSeat?.ShowtimeSeatShowtime?.ShowtimeMovie?.MovieTitle ?? "Unknown",
                    Screen = t.TicketShowtimeSeat?.ShowtimeSeatShowtime?.ShowtimeScreen?.ScreenName ?? "Unknown",
                    // Group by the specific showtime start datetime
                    StartDateTime = t.TicketShowtimeSeat?.ShowtimeSeatShowtime?.ShowtimeStart
                })
                .Select(g => new
                {
                    MovieTitle = g.Key.Movie.Replace(",", " "), // Escape commas
                    ScreenName = g.Key.Screen,
                    Date = g.Key.StartDateTime?.ToString("yyyy-MM-dd") ?? "N/A",
                    Time = g.Key.StartDateTime?.ToString("HH:mm") ?? "N/A",
                    TicketsSold = g.Count(),
                    Revenue = g.Sum(x => x.TicketPrice)
                })
                .OrderBy(x => x.Date).ThenBy(x => x.Time) // Sort chronologically
                .ToList();

            // 3. Build CSV Content
            var builder = new StringBuilder();
            // Header
            builder.AppendLine("Date,Time,Movie Title,Screen,Tickets Sold,Revenue (VND)");

            foreach (var item in summaryData)
            {
                builder.AppendLine($"{item.Date},{item.Time},{item.MovieTitle},{item.ScreenName},{item.TicketsSold},{item.Revenue.ToString("F0")}");
            }

            // Footer (Grand Total)
            decimal grandTotalRevenue = summaryData.Sum(x => x.Revenue);
            int grandTotalTickets = summaryData.Sum(x => x.TicketsSold);

            builder.AppendLine(",,,,,"); // Spacer
            builder.AppendLine($",,,GRAND TOTAL,{grandTotalTickets},{grandTotalRevenue.ToString("F0")}");

            // 4. Add BOM for Excel/Google Sheets compatibility (UTF-8)
            var content = builder.ToString();
            var encoding = new UTF8Encoding(true);
            var preamble = encoding.GetPreamble();
            var data = encoding.GetBytes(content);
            var finalData = preamble.Concat(data).ToArray();

            return File(finalData, "text/csv", $"Revenue_Summary_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }
    }
}
