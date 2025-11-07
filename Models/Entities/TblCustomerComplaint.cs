using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblCustomerComplaint
{
    public int CustomerComplaintId { get; set; }

    public int? CustomerComplaintCustomerUserId { get; set; }

    public int? CustomerComplaintTenantId { get; set; }

    public string? CustomerComplaintDescription { get; set; }

    public int? CustomerComplaintStatus { get; set; }

    public DateTime? CustomerComplaintCreatedAt { get; set; }

    public virtual TblUser? CustomerComplaintCustomerUser { get; set; }

    public virtual TblTenant? CustomerComplaintTenant { get; set; }
}
