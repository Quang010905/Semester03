using System;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class MyComplaintVm
    {
        public int Id { get; set; }

        // "Movie", "Event", "Store", ...
        public string TargetType { get; set; } = "";

        // Tên phim, tên event, hoặc tên tenant
        public string TargetName { get; set; } = "";

        public int? Rate { get; set; }

        public string StatusLabel { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public string ShortContent { get; set; } = "";

        // Link xem chi tiết nếu sau này bạn muốn làm
        public string DetailUrl { get; set; } = "";
    }
}
