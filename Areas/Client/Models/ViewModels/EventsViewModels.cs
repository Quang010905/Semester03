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
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
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

        // --- NEW: quick organizer/position for list cards ---
        public int OrganizerId { get; set; } = 0;
        public string OrganizerName { get; set; } = "-";
        public string PositionName { get; set; } = "-";
        public decimal? Price { get; set; }
    }

    // ==========================
    // HOME VM (DÙNG CHO INDEX)
    // ==========================
    public class EventHomeVm
    {
        public List<EventCardVm> Upcoming { get; set; } = new();
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

        // --- Organizer / Tenant (shop) info ---
        public string OrganizerShopName { get; set; } = "-";      // Tenant.TenantName (shop)
        public string OrganizerImageUrl { get; set; } = "";
        public string OrganizerDescription { get; set; } = "";
        public string OrganizerEmail { get; set; } = "";
        public string OrganizerPhone { get; set; } = "";

        // --- Position details (from Tbl_TenantPosition) ---
        public string PositionLocation { get; set; } = "";       // TenantPosition_Location
        public int? PositionFloor { get; set; } = null;          // TenantPosition_Floor

        public decimal? Price { get; set; }            // giá 1 vé, null => miễn phí
        public int AvailableSlots { get; set; } = 0;   // số còn lại (MaxSlot - đã booked)

        public List<EventCardVm> Related { get; set; } = new();

        public List<CommentVm> Comments { get; set; } = new();
        public double AvgRate { get; set; }            // mặc định 0 nếu chưa có comment
        public int CommentCount { get; set; }

        public bool IsPast { get; set; }
        public bool IsOngoing { get; set; }
        public bool IsUpcoming { get; set; }
        public bool IsActive { get; set; }
    }

    // ===============================================================
    // BOOKING MODELS
    // ===============================================================

    public class EventRegisterVm
    {
        public EventDetailsVm Event { get; set; }
        public int AvailableSlots { get; set; }
    }

    public class EventBookingVm
    {
        public int EventId { get; set; }
        public int Slot { get; set; }

        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class EventBookingSuccessVm
    {
        public int BookingId { get; set; }
        public string EventTitle { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
        public int PaymentStatus { get; set; }
        public string ContactEmail { get; set; }
    }

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

    public class UserEventBookingVm
    {
        public int BookingId { get; set; }
        public int EventId { get; set; }

        public string EventTitle { get; set; }
        public string EventImage { get; set; }

        public int Slot { get; set; }
        public DateTime CreatedAt { get; set; }
    }

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
