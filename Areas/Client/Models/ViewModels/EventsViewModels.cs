using System;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Models.ViewModels
{
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
        public string TenantName { get; set; }        // Tên đơn vị/địa điểm tổ chức
    }

    // ==========================
    // HOME VM
    // ==========================
    public class EventHomeVm
    {
        public List<EventCardVm> Featured { get; set; } = new();
        public List<EventCardVm> Upcoming { get; set; } = new();
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
        public string TenantName { get; set; }        // Tên đơn vị/địa điểm tổ chức

        public List<EventCardVm> Related { get; set; } = new();
    }

    // ===============================================================
    // BOOKING MODELS
    // ===============================================================

    // Trang đăng ký (hiển thị Event + slot còn trống)
    public class EventRegisterVm
    {
        public EventDetailsVm Event { get; set; }
        public int AvailableSlots { get; set; }
    }

    // Model submit booking (dùng nếu bạn muốn binding dạng object)
    public class EventBookingVm
    {
        public int EventId { get; set; }
        public DateTime EventDate { get; set; }       // Ngày tham dự sự kiện (EventBooking_Date – có thể dùng sau)
        public int Slot { get; set; }

        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // View hiển thị sau khi booking thành công
    public class EventBookingSuccessVm
    {
        public int BookingId { get; set; }
        public int? OrderGroup { get; set; }          // Mã nhóm đơn hàng (nếu có)
        public string EventTitle { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
        public string ContactEmail { get; set; }

        public int PaymentStatus { get; set; }        // Trạng thái thanh toán
    }

    // Chi tiết một booking (cho user/admin – bạn sẽ dùng sau)
    public class EventBookingDetailsVm
    {
        public int BookingId { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; }
        public string EventImage { get; set; }

        public DateTime EventDate { get; set; }       // Ngày tham dự sự kiện
        public int Slot { get; set; }
        public decimal UnitPrice { get; set; }        // Đơn giá mỗi vé/slot
        public decimal TotalAmount => UnitPrice * Slot;

        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }

        public int PaymentStatus { get; set; }
        public int? OrderGroup { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Danh sách booking của user (Profile – bạn sẽ dùng sau)
    public class UserEventBookingVm
    {
        public int BookingId { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; }
        public string EventImage { get; set; }

        public DateTime EventDate { get; set; }
        public int Slot { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount => UnitPrice * Slot;

        public int PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Dùng cho Admin (nếu muốn – hiện tại không đụng vào Admin controller)
    public class EventBookingListVm
    {
        public int BookingId { get; set; }
        public string EventTitle { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }

        public DateTime EventDate { get; set; }
        public int Slot { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount => UnitPrice * Slot;

        public int PaymentStatus { get; set; }
        public int? OrderGroup { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
