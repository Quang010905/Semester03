using System;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class BookingConfirmVm
    {
        public int ShowtimeId { get; set; }
        public string MovieTitle { get; set; } = "";
        public string CinemaName { get; set; } = "";
        public string ScreenName { get; set; } = "";
        public DateTime ShowtimeStart { get; set; }
        public List<string> SelectedSeats { get; set; } = new();
        public decimal SeatPrice { get; set; } = 0m;
        public decimal TotalAmount { get; set; } = 0m;

        public List<CouponDto> AvailableCoupons { get; set; } = new();
        public int UserPoints { get; set; } = 0;
    }
}
