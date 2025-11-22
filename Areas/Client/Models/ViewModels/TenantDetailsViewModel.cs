using System;
using System.Collections.Generic;

namespace Semester03.Models.ViewModels
{
    public class TenantDetailsViewModel
    {
        public int TenantId { get; set; }

        public string TenantName { get; set; } = "";
        public string? TenantImg { get; set; }        // store image
        public string TenantDescription { get; set; } = "";
        public string TenantTypeName { get; set; } = "";

        /// <summary>
        /// Store location / unit code (e.g. "F1-A03").
        /// </summary>
        public string Position { get; set; } = "";

        /// <summary>
        /// Average rating (1–5). Can be null if no comments.
        /// </summary>
        public double? AvgRate { get; set; }

        /// <summary>
        /// Product categories of this tenant.
        /// </summary>
        public List<ProductCategoryVm> ProductCategories { get; set; } = new();

        /// <summary>
        /// List of customer comments.
        /// </summary>
        public List<CustomerCommentVm> Comments { get; set; } = new();

        /// <summary>
        /// Current promotions of this tenant.
        /// </summary>
        public List<TenantPromotionVm> Promotions { get; set; } = new();
    }

    public class CustomerCommentVm
    {
        public string UserName { get; set; } = "";
        public string Text { get; set; } = "";
        public int Rate { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class ProductVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Img { get; set; }
        public decimal? Price { get; set; }
    }

    public class ProductCategoryVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Img { get; set; }
    }

    public class TenantPromotionVm
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Img { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? MinBillAmount { get; set; }
        public string Description { get; set; } = "";
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
    }
}
