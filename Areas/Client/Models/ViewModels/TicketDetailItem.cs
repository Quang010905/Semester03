namespace Semester03.Areas.Client.Models.ViewModels
{
    public class TicketDetailItem
    {
        public string MovieTitle { get; set; }
        public string CinemaName { get; set; }
        public string ScreenName { get; set; }
        public DateTime ShowtimeStart { get; set; }
        public string SeatLabel { get; set; }
        public decimal Price { get; set; }
    }
}
