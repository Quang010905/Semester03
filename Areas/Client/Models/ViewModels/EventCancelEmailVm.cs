namespace Semester03.Areas.Client.Models.ViewModels
{
    public class EventCancelEmailVm
    {
        public string UserName { get; set; }
        public string EventName { get; set; }
        public int BookingId { get; set; }
        public int CancelledQty { get; set; }
        public int RemainingQty { get; set; }
        public decimal RefundAmount { get; set; }
    }
}
