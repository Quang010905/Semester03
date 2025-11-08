namespace Semester03.Areas.Admin.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalTenants { get; set; } // Tổng số cửa hàng
        public int TotalMovies { get; set; }  // Tổng số phim
        public int TotalTicketsSold { get; set; } // Tổng số vé
    }
}
