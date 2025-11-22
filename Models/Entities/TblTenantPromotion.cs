using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblTenantPromotion
{
    public int TenantPromotionId { get; set; }

    public int TenantPromotionTenantId { get; set; }

    public string TenantPromotionTitle { get; set; } = null!;

    public string? TenantPromotionImg { get; set; }

    public string? TenantPromotionDescription { get; set; }

    public decimal? TenantPromotionDiscountPercent { get; set; }

    public decimal? TenantPromotionDiscountAmount { get; set; }

    public decimal? TenantPromotionMinBillAmount { get; set; }

    public DateTime TenantPromotionStart { get; set; }

    public DateTime TenantPromotionEnd { get; set; }

    public int? TenantPromotionStatus { get; set; }

    public virtual TblTenant TenantPromotionTenant { get; set; } = null!;
}
