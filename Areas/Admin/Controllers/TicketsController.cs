using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using Semester03.Services.Email;
using System.Text;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class TicketsController : Controller
    {
        private readonly TicketRepository _ticketRepo;
        private readonly AbcdmallContext _context;
        private readonly TicketEmailService _emailService;

        public TicketsController(TicketRepository ticketRepo, AbcdmallContext context, TicketEmailService emailService)
        {
            _ticketRepo = ticketRepo;
            _context = context;
            _emailService = emailService;
        }

        // GET: Admin/Tickets
        public async Task<IActionResult> Index(
            string search,
            DateTime? fromDate,
            DateTime? toDate,
            string status) // [NEW] status
        {
            // Mặc định: Nếu chưa chọn ngày thì lấy 30 ngày gần nhất (để không load quá nặng)
             if (!fromDate.HasValue) fromDate = DateTime.Now.AddDays(-30);

            var tickets = await _ticketRepo.SearchTicketsAsync(search, null, fromDate, toDate, status);

            // Lưu lại giá trị để hiển thị trên View
            ViewData["CurrentSearch"] = search;
            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");
            ViewData["CurrentStatus"] = status;

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
            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null) return NotFound();

            var showTime = ticket.TicketShowtimeSeat?.ShowtimeSeatShowtime?.ShowtimeStart;

            if (showTime.HasValue && showTime.Value <= DateTime.Now)
            {
                TempData["Error"] = "Cannot cancel this ticket because the showtime has already started or ended.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var result = await _ticketRepo.CancelTicketAsync(id);

            if (result)
            {
                try
                {
                    var userId = ticket.TicketBuyerUserId;
                    var movieName = ticket.TicketShowtimeSeat?.ShowtimeSeatShowtime?.ShowtimeMovie?.MovieTitle;
                    var seatLabel = ticket.TicketShowtimeSeat?.ShowtimeSeatSeat?.SeatLabel;

                    // Call TicketEmailService
                    await _emailService.SendMovieCancelEmailAsync(
                        userId,
                        movieName ?? "Movie",
                        showTime ?? DateTime.Now,
                        new List<string> { seatLabel ?? "N/A" },
                        ticket.TicketPrice
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Email Error: " + ex.Message);
                }

                TempData["Success"] = $"Ticket #{id} cancelled successfully. Refund email sent.";
            }
            else
            {
                TempData["Error"] = $"Could not cancel Ticket #{id}. It might already be cancelled or an error occurred.";
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAllTickets(int id)
        {
            var showtime = await _context.TblShowtimes.FindAsync(id);
            if (showtime == null) return NotFound();

            if (showtime.ShowtimeStart <= DateTime.Now)
            {
                TempData["Error"] = "Cannot cancel tickets: The showtime has already started or ended!";
                return RedirectToAction("Details", "Showtimes", new { id = id });
            }

            var soldTickets = await _context.TblTickets
                .Include(t => t.TicketBuyerUser)
                .Include(t => t.TicketShowtimeSeat)
                    .ThenInclude(ss => ss.ShowtimeSeatSeat) // Get seat count
                .Include(t => t.TicketShowtimeSeat)
                    .ThenInclude(ss => ss.ShowtimeSeatShowtime)
                        .ThenInclude(s => s.ShowtimeMovie) // Get Movie Name
                .Where(t => t.TicketShowtimeSeat.ShowtimeSeatShowtimeId == id
                            && t.TicketStatus == "sold") 
                .ToListAsync();

            if (!soldTickets.Any())
            {
                TempData["Info"] = "No sold tickets found to cancel.";
                return RedirectToAction("Details", "Showtimes", new { id = id });
            }

            // 'id' here is the ShowtimeId
            var result = await _ticketRepo.CancelAllTicketsForShowtimeAsync(id);

            if (result > 0)
            {
                var customerGroups = soldTickets.GroupBy(t => t.TicketBuyerUserId);
                int mailCount = 0;

                foreach (var group in customerGroups)
                {
                    try
                    {
                        var firstItem = group.First();
                        var userId = group.Key;
                        var movieName = firstItem.TicketShowtimeSeat?.ShowtimeSeatShowtime?.ShowtimeMovie?.MovieTitle;
                        var sTime = firstItem.TicketShowtimeSeat?.ShowtimeSeatShowtime?.ShowtimeStart ?? DateTime.Now;

                        // Gom danh sách ghế: "A1, A2, B5"
                        var seats = group.Select(t => t.TicketShowtimeSeat?.ShowtimeSeatSeat?.SeatLabel).ToList();

                        // Tổng tiền hoàn
                        var totalRefund = group.Sum(t => t.TicketPrice);

                        await _emailService.SendMovieCancelEmailAsync(
                            userId, movieName, sTime, seats, totalRefund
                        );
                        mailCount++;
                    }
                    catch { /* Ignore mail error */ }
                }

                TempData["Success"] = $"Cancelled {result} tickets. Sent refund emails to {mailCount} customers.";
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
        public async Task<IActionResult> ExportSummary(
            string search,
            DateTime? fromDate,
            DateTime? toDate,
            string status)
        {
            // 1. Get Raw Data
            var tickets = await _ticketRepo.SearchTicketsAsync(search, null, fromDate, toDate, status);
            var soldTickets = tickets.Where(t => t.TicketStatus.ToLower() == "sold").ToList();

            // 2. Prepare reporting data
            // A. Movie Report (Top Revenue)
            var revenueByMovie = soldTickets
                .GroupBy(t => t.TicketShowtimeSeat?.ShowtimeSeatShowtime?.ShowtimeMovie?.MovieTitle ?? "Unknown")
                .Select(g => new
                {
                    Movie = g.Key,
                    Tickets = g.Count(),
                    Revenue = g.Sum(t => t.TicketPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // B. Details Report 
            var detailedList = soldTickets
                .Select(t => new
                {
                    Date = t.TicketCreatedAt?.ToString("yyyy-MM-dd"),
                    Time = t.TicketCreatedAt?.ToString("HH:mm"),
                    Movie = t.TicketShowtimeSeat?.ShowtimeSeatShowtime?.ShowtimeMovie?.MovieTitle,
                    Screen = t.TicketShowtimeSeat?.ShowtimeSeatShowtime?.ShowtimeScreen?.ScreenName,
                    Seat = t.TicketShowtimeSeat?.ShowtimeSeatSeat?.SeatLabel,
                    Price = t.TicketPrice
                })
                .OrderByDescending(x => x.Date).ThenBy(x => x.Time)
                .ToList();

            // 3. CREATE EXCEL FILE
            using (var workbook = new XLWorkbook())
            {
                // === SHEET 1:(SUMMARY) ===
                var wsSummary = workbook.Worksheets.Add("Aggregate Revenue");

                // Header Title
                wsSummary.Cell(1, 1).Value = "REVENUE REPORT BY FILM";
                wsSummary.Range(1, 1, 1, 3).Merge().Style.Font.Bold = true;
                wsSummary.Range(1, 1, 1, 3).Style.Font.FontSize = 14;
                wsSummary.Range(1, 1, 1, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Header Table
                wsSummary.Cell(3, 1).Value = "Movie Name";
                wsSummary.Cell(3, 2).Value = "Sold Ticket";
                wsSummary.Cell(3, 3).Value = "Revenue (VND)";
                wsSummary.Range(3, 1, 3, 3).Style.Fill.BackgroundColor = XLColor.CornflowerBlue; 
                wsSummary.Range(3, 1, 3, 3).Style.Font.FontColor = XLColor.White;
                wsSummary.Range(3, 1, 3, 3).Style.Font.Bold = true;

                // Render Data
                int row = 4;
                foreach (var item in revenueByMovie)
                {
                    wsSummary.Cell(row, 1).Value = item.Movie;
                    wsSummary.Cell(row, 2).Value = item.Tickets;
                    wsSummary.Cell(row, 3).Value = item.Revenue;
                    wsSummary.Cell(row, 3).Style.NumberFormat.Format = "#,##0"; // Format currency
                    row++;
                }

                // Total Row
                wsSummary.Cell(row, 1).Value = "TOTAL";
                wsSummary.Cell(row, 2).FormulaA1 = $"SUM(B4:B{row - 1})";
                wsSummary.Cell(row, 3).FormulaA1 = $"SUM(C4:C{row - 1})";
                wsSummary.Range(row, 1, row, 3).Style.Font.Bold = true;
                wsSummary.Range(row, 1, row, 3).Style.Fill.BackgroundColor = XLColor.LightGray;
                wsSummary.Cell(row, 3).Style.NumberFormat.Format = "#,##0";

                wsSummary.Columns().AdjustToContents(); // Auto-fit columns

                // === SHEET 2: (DETAILS) ===
                var wsDetail = workbook.Worksheets.Add("Transaction Details");

                // Header
                var headers = new[] { "Day", "Time", "Movie", "Screen", "Seat", "Price (VND)" };
                for (int i = 0; i < headers.Length; i++)
                {
                    wsDetail.Cell(1, i + 1).Value = headers[i];
                }
                wsDetail.Range(1, 1, 1, 6).Style.Font.Bold = true;
                wsDetail.Range(1, 1, 1, 6).Style.Fill.BackgroundColor = XLColor.DarkGreen;
                wsDetail.Range(1, 1, 1, 6).Style.Font.FontColor = XLColor.White;

                // Data
                int dRow = 2;
                foreach (var item in detailedList)
                {
                    wsDetail.Cell(dRow, 1).Value = item.Date;
                    wsDetail.Cell(dRow, 2).Value = item.Time;
                    wsDetail.Cell(dRow, 3).Value = item.Movie;
                    wsDetail.Cell(dRow, 4).Value = item.Screen;
                    wsDetail.Cell(dRow, 5).Value = item.Seat;
                    wsDetail.Cell(dRow, 6).Value = item.Price;
                    wsDetail.Cell(dRow, 6).Style.NumberFormat.Format = "#,##0";
                    dRow++;
                }
                wsDetail.Columns().AdjustToContents();

                // 4. Export file to MemoryStream
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"RevenueReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
    }
}
