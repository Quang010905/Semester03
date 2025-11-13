using System;
using System.Collections.Generic;

namespace Semester03.Models.ViewModels
{
    public class TenantDetailsViewModel
    {
        public int TenantId { get; set; }
        public string TenantName { get; set; } = "";
        public string? TenantImg { get; set; }      // hình ảnh cửa hàng
        public string TenantDescription { get; set; } = "";
        public string TenantTypeName { get; set; } = "";
        public string Position { get; set; } = "";

        // ✅ Rating trung bình
        public double? AvgRate { get; set; }           // có thể null nếu chưa có comment

        // ✅ Danh mục sản phẩm
        public List<ProductCategoryVm> ProductCategories { get; set; } = new List<ProductCategoryVm>();

        // ✅ Danh sách bình luận
        public List<CustomerCommentVm> Comments { get; set; } = new List<CustomerCommentVm>();
    }

    public class CustomerCommentVm
    {
        public string UserName { get; set; } = "";  // tên người comment
        public string Text { get; set; } = "";      // nội dung comment
        public int Rate { get; set; }               // điểm rating
        public DateTime? CreatedAt { get; set; }    // ngày comment
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
        public string? Img { get; set; }            // ảnh đại diện danh mục
    }
}
