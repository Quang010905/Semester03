namespace Semester03.Areas.Client.Models.ViewModels
{
    public class MovieCancelEmailVm
    {
        public string UserName { get; set; }
        public string MovieName { get; set; }
        public DateTime Showtime { get; set; }
        public List<string> CancelledSeats { get; set; }
        public decimal RefundAmount { get; set; }
    }
}
