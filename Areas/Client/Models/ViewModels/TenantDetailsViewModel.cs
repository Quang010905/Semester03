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

        // Rating trung bình
        public double? AvgRate { get; set; }           // có thể null nếu chưa có comment

        // Danh sách bình luận
        public List<CustomerCommentVm> Comments { get; set; } = new List<CustomerCommentVm>();
    }

    public class CustomerCommentVm
    {
        public string UserName { get; set; } = "";  // tên người comment
        public string Text { get; set; } = "";      // nội dung comment
        public int Rate { get; set; }               // điểm rating
        public DateTime? CreatedAt { get; set; }    // ngày comment
    }
}
