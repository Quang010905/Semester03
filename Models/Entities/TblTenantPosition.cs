using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblTenantPosition
{
    public int TenantPositionId { get; set; }

    public string TenantPositionLocation { get; set; } = null!;

    public int TenantPositionFloor { get; set; }

    public decimal TenantPositionAreaM2 { get; set; }

    public decimal TenantPositionRentPricePerM2 { get; set; }

    public int? TenantPositionStatus { get; set; }

    public int? TenantPositionAssignedTenantId { get; set; }

    public virtual ICollection<TblEvent> TblEvents { get; set; } = new List<TblEvent>();

    public virtual TblTenant? TenantPositionAssignedTenant { get; set; }
}
