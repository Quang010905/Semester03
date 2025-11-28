using System;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Models.ViewModels
{
    // ViewModel cho từng ghế trong suất chiếu
    public class TicketSeatVm
    {
        public int TicketId { get; set; }
        public string SeatLabel { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty; // active / cancelled
    }

    // ViewModel cho trang chi tiết vé
    public class TicketDetailVm
    {
        public int ShowtimeId { get; set; }

        public int TicketId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string MovieImg { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string Director { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public DateTime Showtime { get; set; }
        public DateTime EndTime { get; set; }

        public string Screen { get; set; } = string.Empty;

        public string TheaterName { get; set; } = string.Empty;
        public string TheaterAddress { get; set; } = string.Empty;

        public string QRCodeUrl { get; set; } = string.Empty;
        public bool IsUsed { get; set; }

        // ⭐ Danh sách ghế đang còn hiệu lực (chưa hủy)
        public List<TicketSeatVm> Seats { get; set; } = new();

        // ⭐ Danh sách ghế đã hủy
        public List<TicketSeatVm> CancelledSeats { get; set; } = new();
    }
}
