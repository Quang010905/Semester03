namespace Semester03.Areas.Admin.Models
{
    public class RevenueReportViewModel
    {
        // 1. (Card Stats)
        public decimal TotalMovieRevenue { get; set; }
        public decimal TotalEventRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalTicketsSold { get; set; }

        // 2. (Line Chart)
        public List<string> ChartLabels { get; set; } // Example: ["20/11", "21/11"...]
        public List<decimal> MovieChartData { get; set; }
        public List<decimal> EventChartData { get; set; }

        // 3. (Bar/Pie Chart)
        public List<string> TopMovieNames { get; set; }
        public List<decimal> TopMovieRevenue { get; set; }

        public List<string> TopEventNames { get; set; }
        public List<decimal> TopEventRevenue { get; set; }

        // 4. (Occupancy)
        public double AverageOccupancyRate { get; set; }
    }
}
