using Semester03.Models.Entities;
using System;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class UserProfileVm
    {
        public TblUser User { get; set; }

        public List<MovieTicketHistoryVm> MovieTickets { get; set; } = new();
        public List<ComplaintHistoryVm> Complaints { get; set; } = new();
        public List<EventBookingHistoryVm> EventBookings { get; set; } = new();
    }

    public class MovieTicketHistoryVm
    {
        public int TicketId { get; set; }
        public string MovieTitle { get; set; }
        public DateTime ShowtimeStart { get; set; }
        public string SeatLabel { get; set; }
        public string ScreenName { get; set; }
        public decimal TicketPrice { get; set; }
        public string TicketStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ComplaintHistoryVm
    {
        public int ComplaintId { get; set; }
        public int Rate { get; set; }
        public string Description { get; set; }
        public string TargetType { get; set; }
        public string TargetName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EventBookingHistoryVm
    {
        public int BookingId { get; set; }
        public string EventName { get; set; }
        public DateTime? BookingDate { get; set; }
        public int Quantity { get; set; }
        public decimal TotalCost { get; set; }
        public int PaymentStatus { get; set; }
        public string LastAction { get; set; }
        public DateTime? LastActionDate { get; set; }
    }

    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? NewPassword { get; set; }   // nếu rỗng thì không đổi password
    }
}
