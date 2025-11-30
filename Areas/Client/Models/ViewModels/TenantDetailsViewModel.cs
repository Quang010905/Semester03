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
        public string Position { get; set; } = "";

        public double? AvgRate { get; set; }
        public List<ProductCategoryVm> ProductCategories { get; set; } = new();
        public List<CustomerCommentVm> Comments { get; set; } = new();
        public int CommentPageIndex { get; set; }      
        public int CommentPageSize { get; set; }       
        public int CommentTotalPages { get; set; }     
        public int CommentCount { get; set; }
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


}
