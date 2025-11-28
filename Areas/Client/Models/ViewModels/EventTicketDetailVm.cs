using System;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class EventTicketDetailVm
    {
        public int BookingId { get; set; }

        public string EventName { get; set; } = "";
        public string EventImg { get; set; } = "";
        public string Description { get; set; } = "";

        public DateTime EventStart { get; set; }
        public DateTime EventEnd { get; set; }

        public int Quantity { get; set; }
        public decimal TotalCost { get; set; }
        public decimal UnitPrice { get; set; }

        public string Status { get; set; } = "";

        // Thông tin địa điểm / đơn vị tổ chức
        public string PositionLocation { get; set; } = "";
        public int? PositionFloor { get; set; }
        public string OrganizerShopName { get; set; } = "";

        // QR check-in
        public string QRCodeUrl { get; set; } = "";

    }
}
