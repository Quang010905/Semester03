using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblRole
{
    public int RolesId { get; set; }

    public string RolesName { get; set; } = null!;

    public string? RolesDescription { get; set; }

    public virtual ICollection<TblUser> TblUsers { get; set; } = new List<TblUser>();
}
