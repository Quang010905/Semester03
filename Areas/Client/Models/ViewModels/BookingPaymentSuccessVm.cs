using System;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class BookingPaymentSuccessVm
    {
        public int ShowtimeId { get; set; }
        public List<string> SeatLabels { get; set; } = new List<string>();
        public decimal PricePerSeat { get; set; }
        public decimal TotalAmount { get; set; }

        // Optional: provide list of created ticket ids or details for further processing
        public List<int> CreatedTicketIds { get; set; } = new List<int>();
    }
}
