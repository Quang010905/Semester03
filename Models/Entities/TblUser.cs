using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblUser
{
    public int UsersId { get; set; }

    public string UsersUsername { get; set; } = null!;

    public string UsersPassword { get; set; } = null!;

    public string UsersFullName { get; set; } = null!;

    public string UsersEmail { get; set; } = null!;

    public string UsersPhone { get; set; } = null!;

    public int UsersRoleId { get; set; }

    public int? UsersStatus { get; set; } 

    public int? UsersPoints { get; set; }

    public DateTime? UsersCreatedAt { get; set; }

    public DateTime? UsersUpdatedAt { get; set; }

    public string? UsersRoleChangeReason { get; set; }

    public virtual ICollection<TblCouponUser> TblCouponUsers { get; set; } = new List<TblCouponUser>();

    public virtual ICollection<TblCustomerComplaint> TblCustomerComplaints { get; set; } = new List<TblCustomerComplaint>();

    public virtual ICollection<TblNotification> TblNotifications { get; set; } = new List<TblNotification>();

    public virtual ICollection<TblShowtimeSeat> TblShowtimeSeats { get; set; } = new List<TblShowtimeSeat>();

    public virtual ICollection<TblTenant> TblTenants { get; set; } = new List<TblTenant>();

    public virtual ICollection<TblTicket> TblTickets { get; set; } = new List<TblTicket>();

    //public virtual TblRole UsersRole { get; set; } ;
    public virtual TblRole? UsersRole { get; set; }
}
