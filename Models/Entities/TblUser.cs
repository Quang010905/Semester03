using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities
{
    public partial class TblUser
    {
        public TblUser()
        {
            TblTickets = new HashSet<TblTicket>();
            ReservedShowtimeSeats = new HashSet<TblShowtimeSeat>();
            TblCouponUsers = new HashSet<TblCouponUser>();
            TblEventBookings = new HashSet<TblEventBooking>();
            TblCustomerComplaints = new HashSet<TblCustomerComplaint>();
            TblNotifications = new HashSet<TblNotification>();
            TblTenants = new HashSet<TblTenant>();
        }

        public int UsersId { get; set; }
        public string UsersUsername { get; set; } = null!;
        public string UsersPassword { get; set; } = null!;
        public string UsersFullName { get; set; } = null!;
        public string UsersEmail { get; set; } = null!;
        public string UsersPhone { get; set; } = null!;
        public int UsersRoleId { get; set; }
        public int? UsersPoints { get; set; }
        public DateTime? UsersCreatedAt { get; set; }
        public DateTime? UsersUpdatedAt { get; set; }

        // navigation
        public virtual TblRole? UsersRole { get; set; }

        public virtual ICollection<TblTicket> TblTickets { get; set; }
        public virtual ICollection<TblShowtimeSeat> ReservedShowtimeSeats { get; set; } // <-- fixes the ReservedShowtimeSeats error
        public virtual ICollection<TblCouponUser> TblCouponUsers { get; set; }
        public virtual ICollection<TblEventBooking> TblEventBookings { get; set; }
        public virtual ICollection<TblCustomerComplaint> TblCustomerComplaints { get; set; }
        public virtual ICollection<TblNotification> TblNotifications { get; set; }
        public virtual ICollection<TblTenant> TblTenants { get; set; }
    }
}
