using System;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class MyTicketVm
    {
        public int TicketId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;

        public DateTime Showtime { get; set; }

        public string SeatLabel { get; set; } = string.Empty;
        public string ScreenName { get; set; } = string.Empty;

        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // 👇 THÊM DÒNG NÀY
        public string PosterUrl { get; set; } = string.Empty;
    }


}
