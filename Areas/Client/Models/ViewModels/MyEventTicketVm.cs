using System;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class MyEventTicketVm
    {
        public int BookingId { get; set; }
        public string EventName { get; set; } = "";
        public string EventImage { get; set; } = "";
        public DateTime EventStart { get; set; }
        public DateTime EventEnd { get; set; }
        public int Quantity { get; set; }
        public decimal TotalCost { get; set; }
        public string Status { get; set; } = "";
    }
}
