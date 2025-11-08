using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities
{
    public partial class TblTicket
    {
        public int TicketId { get; set; }

        // Khóa tới Tbl_Showtime (bắt buộc)
        public int TicketShowtimeId { get; set; }

        // Khóa tới Tbl_ShowtimeSeat (nullable theo schema bạn cung cấp)
        public int? TicketShowtimeSeatId { get; set; }

        // Tên/label ghế (ví dụ "C7")
        public string? TicketSeat { get; set; }

        // Người mua (nullable nếu không bắt buộc)
        public int? TicketBuyerUserId { get; set; }

        // Trạng thái ticket: sold / reserved / cancelled ...
        public string? TicketStatus { get; set; }

        // Giá vé
        public decimal? TicketPrice { get; set; }

        // Thời điểm mua
        public DateTime? TicketPurchasedAt { get; set; }

        // (Tùy chọn) thời điểm cập nhật record
        public DateTime? TicketUpdatedAt { get; set; }

        // Navigation properties
        public virtual TblUser? TicketBuyerUser { get; set; }
        public virtual TblShowtime TicketShowtime { get; set; } = null!;

        // Navigation tới ShowTimeSeat nếu bạn muốn tham chiếu trực tiếp
        public virtual TblShowtimeSeat? TicketShowtimeSeat { get; set; }
    }
}
