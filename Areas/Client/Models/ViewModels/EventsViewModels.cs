using System;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Models.ViewModels
{
    // ==========================
    // PagedResult dùng chung
    // ==========================
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();

        /// <summary>Trang hiện tại (1-based)</summary>
        public int PageIndex { get; set; }

        /// <summary>Số item / trang</summary>
        public int PageSize { get; set; }

        /// <summary>Tổng số item</summary>
        public int TotalItems { get; set; }

        public int TotalPages =>
            PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalItems / PageSize);
    }

    // ==========================
    // EVENT CARD (for list)
    // ==========================
    public class EventCardVm
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ShortDescription { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string ImageUrl { get; set; }

        public int? MaxSlot { get; set; }
        public int Status { get; set; }
        public int? TenantPositionId { get; set; }
    }

    // ==========================
    // HOME VM (DÙNG CHO INDEX)
    // ==========================
    // ❌ Bỏ Featured
    // ✅ Có: Upcoming + Past (phân trang)
    public class EventHomeVm
    {
        // Sự kiện sắp / đang diễn ra
        public List<EventCardVm> Upcoming { get; set; } = new();

        // Sự kiện đã diễn ra (có phân trang)
        public PagedResult<EventCardVm> Past { get; set; } = new();
    }

    // ==========================
    // EVENT DETAILS
    // ==========================
    public class EventDetailsVm
    {
        public int Id { get; set; }
        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string ImageUrl { get; set; }

        public int MaxSlot { get; set; }
        public int Status { get; set; }

        public int TenantPositionId { get; set; }

        public List<EventCardVm> Related { get; set; } = new();

        // === COMMENT CHO EVENT (dùng chung CommentVm với Movie) ===
        public List<CommentVm> Comments { get; set; } = new();
        public double AvgRate { get; set; }
        public int CommentCount { get; set; }

        // === FLAG TRẠNG THÁI THEO THỜI GIAN & HOẠT ĐỘNG ===
        public bool IsPast { get; set; }       // Đã kết thúc
        public bool IsOngoing { get; set; }    // Đang diễn ra
        public bool IsUpcoming { get; set; }   // Sắp diễn ra
        public bool IsActive { get; set; }     // EventStatus == 1
    }

    // ===============================================================
    // BOOKING MODELS
    // ===============================================================

    // For client register page (controller expects this)
    public class EventRegisterVm
    {
        public EventDetailsVm Event { get; set; }
        public int AvailableSlots { get; set; }
    }

    // Used when submit booking (you may use instead EventBookingVm)
    public class EventBookingVm
    {
        public int EventId { get; set; }
        public int Slot { get; set; }

        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // Booking success view model (controller expects this)
    public class EventBookingSuccessVm
    {
        public int BookingId { get; set; }
        public string EventTitle { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
        public int PaymentStatus { get; set; }
        public string ContactEmail { get; set; }
    }

    // Dùng hiển thị chi tiết booking
    public class EventBookingDetailsVm
    {
        public int BookingId { get; set; }
        public int EventId { get; set; }

        public string EventTitle { get; set; }
        public string EventImage { get; set; }

        public int Slot { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    // Dùng hiển thị danh sách booking của user
    public class UserEventBookingVm
    {
        public int BookingId { get; set; }
        public int EventId { get; set; }

        public string EventTitle { get; set; }
        public string EventImage { get; set; }

        public int Slot { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Dành cho Admin (nếu cần)
    public class EventBookingListVm
    {
        public int BookingId { get; set; }
        public string EventTitle { get; set; }

        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }

        public int Slot { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
