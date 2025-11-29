using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Admin.Models;
using Semester03.Models.Entities;
using System.Globalization;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize(Roles = "Super Admin, Mall Manager")]
    public class AdminController : Controller
    {
        private readonly AbcdmallContext _context;
        private DateTime? tomorrow;

        public AdminController(AbcdmallContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            var today = now.Date;

            // Thống kê tổng quan
            var totalUsers = await _context.TblUsers.CountAsync();
            var totalTenants = await _context.TblTenants.CountAsync();
            var activeTenants = await _context.TblTenants.CountAsync(t => t.TenantStatus == 1);
            var totalMovies = await _context.TblMovies.CountAsync();
            var activeMovies = await _context.TblMovies.CountAsync(m => m.MovieStatus == 1 && m.MovieStartDate <= now && m.MovieEndDate >= now);
            var upcomingMovies = await _context.TblMovies.CountAsync(m => m.MovieStatus == 1 && m.MovieStartDate > now);

            // Thống kê vé
            var totalTicketsSold = await _context.TblTickets.CountAsync();
            var totalTicketRevenue = await _context.TblTickets.SumAsync(t => (decimal?)t.TicketPrice) ?? 0;
            var monthlyTicketRevenue = await _context.TblTickets
                .Where(t => t.TicketCreatedAt >= firstDayOfMonth)
                .SumAsync(t => (decimal?)t.TicketPrice) ?? 0;

            // Thống kê sự kiện
            var totalEvents = await _context.TblEvents.CountAsync();
            var totalEventRevenue = await _context.TblEventBookings
                .Where(eb => eb.EventBookingPaymentStatus == 1)
                .SumAsync(eb => (decimal?)eb.EventBookingTotalCost) ?? 0;
            var monthlyEventRevenue = await _context.TblEventBookings
                .Where(eb => eb.EventBookingPaymentStatus == 1 && eb.EventBookingCreatedDate >= firstDayOfMonth)
                .SumAsync(eb => (decimal?)eb.EventBookingTotalCost) ?? 0;
            var todayEventBookings = await _context.TblEventBookings
     .CountAsync(eb => eb.EventBookingCreatedDate >= today && eb.EventBookingCreatedDate < tomorrow);
            // Thống kê khiếu nại
            var pendingComplaints = await _context.TblCustomerComplaints
                .CountAsync(c => c.CustomerComplaintStatus == 0);

            // Thống kê suất chiếu hôm nay
            var todayShowtimes = await _context.TblShowtimes
                .CountAsync(s => s.ShowtimeStart.Date == today);

            // Thống kê bãi đỗ xe
            var totalParkingSpots = await _context.TblParkingSpots.CountAsync();
            var occupiedParkingSpots = await _context.TblParkingSpots.CountAsync(p => p.SpotStatus == 1);
            var parkingOccupancyRate = totalParkingSpots > 0
                ? (decimal)occupiedParkingSpots / totalParkingSpots * 100
                : 0;

            // Top 5 phim bán chạy nhất
            var topMovies = await _context.TblTickets
                .Include(t => t.TicketShowtimeSeat)
                    .ThenInclude(ss => ss.ShowtimeSeatShowtime)
                        .ThenInclude(st => st.ShowtimeMovie)
                .GroupBy(t => new {
                    t.TicketShowtimeSeat.ShowtimeSeatShowtime.ShowtimeMovie.MovieId,
                    t.TicketShowtimeSeat.ShowtimeSeatShowtime.ShowtimeMovie.MovieTitle
                })
                .Select(g => new TopMovieDto
                {
                    MovieTitle = g.Key.MovieTitle,
                    TicketsSold = g.Count(),
                    Revenue = g.Sum(t => t.TicketPrice)
                })
                .OrderByDescending(x => x.TicketsSold)
                .Take(5)
                .ToListAsync();

            // Thống kê loại cửa hàng
            var tenantTypeStats = await _context.TblTenants
                .Include(t => t.TenantType)
                .GroupBy(t => t.TenantType.TenantTypeName)
                .Select(g => new TenantTypeStatDto
                {
                    TypeName = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // Gán màu cho từng loại
            var colors = new[] { "#4e73df", "#1cc88a", "#36b9cc", "#f6c23e", "#e74a3b", "#858796" };
            for (int i = 0; i < tenantTypeStats.Count; i++)
            {
                tenantTypeStats[i].Color = colors[i % colors.Length];
            }

            // Doanh thu 6 tháng gần nhất
            var monthlyRevenue = new List<MonthlyRevenueDto>();
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                var ticketRev = await _context.TblTickets
                    .Where(t => t.TicketCreatedAt >= monthStart && t.TicketCreatedAt < monthEnd)
                    .SumAsync(t => (decimal?)t.TicketPrice) ?? 0;

                var eventRev = await _context.TblEventBookings
                    .Where(eb => eb.EventBookingPaymentStatus == 1
                        && eb.EventBookingCreatedDate >= monthStart
                        && eb.EventBookingCreatedDate < monthEnd)
                    .SumAsync(eb => (decimal?)eb.EventBookingTotalCost) ?? 0;

                monthlyRevenue.Add(new MonthlyRevenueDto
                {
                    Month = monthStart.ToString("MMM yyyy", new CultureInfo("vi-VN")),
                    TicketRevenue = ticketRev,
                    EventRevenue = eventRev
                });
            }

            // Vé bán ra trong 7 ngày gần nhất
            var dailyTicketSales = new List<DailyTicketSalesDto>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var nextDate = date.AddDays(1);

                var count = await _context.TblTickets
                    .CountAsync(t => t.TicketCreatedAt >= date && t.TicketCreatedAt < nextDate);

                dailyTicketSales.Add(new DailyTicketSalesDto
                {
                    Date = date.ToString("dd/MM"),
                    TicketCount = count
                });
            }

            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalTenants = totalTenants,
                ActiveTenants = activeTenants,
                TotalMovies = totalMovies,
                ActiveMovies = activeMovies,
                UpcomingMovies = upcomingMovies,
                TotalTicketsSold = totalTicketsSold,
                TotalEvents = totalEvents,
                TotalParkingSpots = totalParkingSpots,
                OccupiedParkingSpots = occupiedParkingSpots,
                ParkingOccupancyRate = parkingOccupancyRate,

                TotalTicketRevenue = totalTicketRevenue,
                TotalEventRevenue = totalEventRevenue,
                MonthlyRevenue = monthlyTicketRevenue + monthlyEventRevenue,

                TodayShowtimes = todayShowtimes,
                PendingComplaints = pendingComplaints,
                TodayEventBookings = todayEventBookings,

                TopMovies = topMovies,
                TenantTypeStats = tenantTypeStats,
                MonthlyRevenueChart = monthlyRevenue,
                DailyTicketSales = dailyTicketSales
            };

            return View(viewModel);
        }
    }
}