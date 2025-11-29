namespace Semester03.Areas.Admin.Models
{
    public class AdminDashboardViewModel
    {
        // Thống kê tổng quan
        public int TotalUsers { get; set; }
        public int TotalTenants { get; set; }
        public int TotalMovies { get; set; }
        public int TotalTicketsSold { get; set; }
        public int TotalEvents { get; set; }
        public int TotalParkingSpots { get; set; }
        public int OccupiedParkingSpots { get; set; }

        // Doanh thu
        public decimal TotalTicketRevenue { get; set; }
        public decimal TotalEventRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }

        // Thống kê chi tiết
        public int ActiveMovies { get; set; }
        public int UpcomingMovies { get; set; }
        public int TodayShowtimes { get; set; }
        public int ActiveTenants { get; set; }
        public int PendingComplaints { get; set; }
        public int TodayEventBookings { get; set; }

        // Top lists
        public List<TopMovieDto> TopMovies { get; set; } = new List<TopMovieDto>();
        public List<TenantTypeStatDto> TenantTypeStats { get; set; } = new List<TenantTypeStatDto>();
        public List<MonthlyRevenueDto> MonthlyRevenueChart { get; set; } = new List<MonthlyRevenueDto>();
        public List<DailyTicketSalesDto> DailyTicketSales { get; set; } = new List<DailyTicketSalesDto>();

        // Parking
        public decimal ParkingOccupancyRate { get; set; }
    }

    public class TopMovieDto
    {
        public string MovieTitle { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TenantTypeStatDto
    {
        public string TypeName { get; set; }
        public int Count { get; set; }
        public string Color { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; }
        public decimal TicketRevenue { get; set; }
        public decimal EventRevenue { get; set; }
    }

    public class DailyTicketSalesDto
    {
        public string Date { get; set; }
        public int TicketCount { get; set; }
    }
}