using System;

namespace Semester03.Models.ViewModels
{
    public class TenantPromotionVm
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Img { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? MinBillAmount { get; set; }
        public string Description { get; set; } = "";
        public DateTime? PromotionStart { get; set; }
        public DateTime? PromotionEnd { get; set; }
    }
}
