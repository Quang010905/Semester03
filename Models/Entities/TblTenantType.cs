using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblTenantType
{
    public int TenantTypeId { get; set; }

    public string? TenantTypeName { get; set; }

    public int? TenantTypeStatus { get; set; }

    public virtual ICollection<TblTenant> TblTenants { get; set; } = new List<TblTenant>();
}
