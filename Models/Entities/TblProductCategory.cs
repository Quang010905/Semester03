using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblProductCategory
{
    public int ProductCategoryId { get; set; }

    public string ProductCategoryName { get; set; } = null!;

    public string ProductCategoryImg { get; set; } = null!;

    public int? ProductCategoryStatus { get; set; }

    public int ProductCategoryTenantId { get; set; }

    public virtual TblTenant ProductCategoryTenant { get; set; } = null!;

    public virtual ICollection<TblProduct> TblProducts { get; set; } = new List<TblProduct>();
}
