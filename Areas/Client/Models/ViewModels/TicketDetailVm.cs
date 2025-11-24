using System;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class TicketDetailVm
    {
        public int TicketId { get; set; }
        public string MovieTitle { get; set; }
        public string MovieImg { get; set; }
        public int Duration { get; set; }
        public string Director { get; set; }
        public string Genre { get; set; }
        public string Description { get; set; }

        public DateTime Showtime { get; set; }
        public DateTime EndTime { get; set; }

        public string Screen { get; set; }
        public string Seat { get; set; }

        public string TheaterName { get; set; }
        public string TheaterAddress { get; set; }

        public string QRCodeUrl { get; set; }
        public bool IsUsed { get; set; }
    }
}
