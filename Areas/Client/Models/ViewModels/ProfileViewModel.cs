using Semester03.Models.Entities;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class ProfileTicketVM
    {
        public int TicketId { get; set; }
        public string MovieTitle { get; set; }
        public string SeatLabel { get; set; }
        public string ScreenName { get; set; }
        public string CinemaName { get; set; }

        // Fix lỗi: nullable
        public DateTime? Showtime { get; set; }

        public decimal Price { get; set; }
    }

    public class ProfileComplaintVM
    {
        public int ComplaintId { get; set; }
        public int Rate { get; set; }
        public string Description { get; set; }

        // Fix lỗi: nullable
        public DateTime? CreatedAt { get; set; }

        public string TargetName { get; set; }
        public int Status { get; set; }
    }

    public class ProfileViewModel
    {
        public TblUser User { get; set; }

        public List<ProfileTicketVM> TicketHistory { get; set; } = new();
        public List<ProfileComplaintVM> ComplaintHistory { get; set; } = new();
    }
}
