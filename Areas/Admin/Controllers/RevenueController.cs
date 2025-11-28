using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Admin.Models;
using Semester03.Models.Entities;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class RevenueController : Controller
    {
        private readonly AbcdmallContext _context;

        public RevenueController(AbcdmallContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new RevenueReportViewModel();

            // Movie: TicketStatus = "sold"
            // Event: EventBookingStatus = 1 

            model.TotalMovieRevenue = await _context.TblTickets
                .Where(t => t.TicketStatus == "sold")
                .SumAsync(t => t.TicketPrice);

            model.TotalEventRevenue = await _context.TblEventBookings
                .Where(e => e.EventBookingStatus == 1)
                .SumAsync(e => e.EventBookingTotalCost ?? 0);

            model.TotalRevenue = model.TotalMovieRevenue + model.TotalEventRevenue;
            model.TotalTicketsSold = await _context.TblTickets.CountAsync(t => t.TicketStatus == "sold");

            // 2. REVENUE CHART (LOST 7 DAYS)
            var today = DateTime.Now.Date;
            var sevenDaysAgo = today.AddDays(-6);

            model.ChartLabels = new List<string>();
            model.MovieChartData = new List<decimal>();
            model.EventChartData = new List<decimal>();

            for (int i = 0; i < 7; i++)
            {
                var date = sevenDaysAgo.AddDays(i);
                model.ChartLabels.Add(date.ToString("dd/MM"));

                // Movie revenue that day
                var movieRev = await _context.TblTickets
                    .Where(t => t.TicketStatus == "sold" && t.TicketCreatedAt.HasValue && t.TicketCreatedAt.Value.Date == date)
                    .SumAsync(t => t.TicketPrice);
                model.MovieChartData.Add(movieRev);

                // Event revenue that day
                var eventRev = await _context.TblEventBookings
                    .Where(e => e.EventBookingStatus == 1 && e.EventBookingCreatedDate.HasValue && e.EventBookingCreatedDate.Value.Date == date)
                    .SumAsync(e => e.EventBookingTotalCost ?? 0);
                model.EventChartData.Add(eventRev);
            }

            // 3. TOP 5 MOVIE 
            var topMovies = await _context.TblTickets
                .Where(t => t.TicketStatus == "sold")
                .Include(t => t.TicketShowtimeSeat)
                    .ThenInclude(ss => ss.ShowtimeSeatShowtime)
                        .ThenInclude(s => s.ShowtimeMovie)
                .GroupBy(t => t.TicketShowtimeSeat.ShowtimeSeatShowtime.ShowtimeMovie.MovieTitle)
                .Select(g => new { Name = g.Key, Revenue = g.Sum(x => x.TicketPrice) })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync();

            model.TopMovieNames = topMovies.Select(x => x.Name).ToList();
            model.TopMovieRevenue = topMovies.Select(x => x.Revenue).ToList();

            // 4. TOP 5 EVENTS
            var topEvents = await _context.TblEventBookings
                .Where(e => e.EventBookingStatus == 1)
                .Include(e => e.EventBookingEvent)
                .GroupBy(e => e.EventBookingEvent.EventName)
                .Select(g => new { Name = g.Key, Revenue = g.Sum(x => x.EventBookingTotalCost ?? 0) })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync();

            model.TopEventNames = topEvents.Select(x => x.Name).ToList();
            model.TopEventRevenue = topEvents.Select(x => x.Revenue).ToList();

            // 5. THEATER FILLING RATE (Occupancy Rate)
            // Formula: Total number of tickets sold / Total number of seats of screenings that have taken place
            var pastShowtimes = _context.TblShowtimes
                .Include(s => s.ShowtimeScreen)
                .Where(s => s.ShowtimeStart < DateTime.Now); 

            if (pastShowtimes.Any())
            {
                // Total seats provided
                long totalSeatsOffered = await pastShowtimes.SumAsync(s => s.ShowtimeScreen.ScreenSeats);

                // Total tickets sold for past shows
                long totalSoldSeats = await _context.TblTickets
                    .Include(t => t.TicketShowtimeSeat)
                        .ThenInclude(ss => ss.ShowtimeSeatShowtime)
                    .CountAsync(t => t.TicketStatus == "sold" && t.TicketShowtimeSeat.ShowtimeSeatShowtime.ShowtimeStart < DateTime.Now);

                if (totalSeatsOffered > 0)
                {
                    model.AverageOccupancyRate = Math.Round(((double)totalSoldSeats / totalSeatsOffered) * 100, 2);
                }
            }

            return View(model);
        }
    }
}
