using System;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class EventBookingEmailVm
    {
        public string UserFullName { get; set; } = string.Empty;
        public int BookingId { get; set; }

        public string EventName { get; set; } = string.Empty;
        public DateTime EventStart { get; set; }
        public DateTime? EventEnd { get; set; }

        public string Location { get; set; } = string.Empty;
        public string OrganizerName { get; set; } = string.Empty;

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }

        public DateTime PurchaseDate { get; set; }
    }
}
