using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblTenant
{
    public int TenantId { get; set; }

    public string TenantName { get; set; } = null!;

    public string TenantImg { get; set; } = null!;

    public int TenantTypeId { get; set; }

    public int TenantUserId { get; set; }

    public string? TenantDescription { get; set; }

    public DateTime? TenantCreatedAt { get; set; }

    public virtual ICollection<TblCustomerComplaint> TblCustomerComplaints { get; set; } = new List<TblCustomerComplaint>();

    public virtual ICollection<TblEventBooking> TblEventBookings { get; set; } = new List<TblEventBooking>();

    public virtual ICollection<TblProductCategory> TblProductCategories { get; set; } = new List<TblProductCategory>();

    public virtual ICollection<TblTenantPosition> TblTenantPositions { get; set; } = new List<TblTenantPosition>();

    public virtual TblTenantType TenantType { get; set; } = null!;

    public virtual TblUser TenantUser { get; set; } = null!;
}
