using System;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class TicketEmailViewModel
    {
        public string UserFullName { get; set; }
        public DateTime PurchaseDate { get; set; }

        public List<TicketEmailItemVm> Tickets { get; set; } = new List<TicketEmailItemVm>();

        // Tổng tiền gốc (chưa giảm)
        public decimal OriginalAmount { get; set; }

        // Số tiền giảm
        public decimal DiscountAmount { get; set; }

        // Tổng tiền cuối cùng (sau giảm) – phải khớp với VNPAY
        public decimal TotalAmount { get; set; }

        public int PointsAwarded { get; set; }
    }

    public class TicketEmailItemVm
    {
        public string MovieTitle { get; set; }
        public string CinemaName { get; set; }
        public string ScreenName { get; set; }
        public DateTime ShowtimeStart { get; set; }
        public string SeatLabel { get; set; }
        public decimal Price { get; set; }
        public string QrCode { get; set; }
    }
}
