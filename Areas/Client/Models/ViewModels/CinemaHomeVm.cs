using System;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class CinemaHomeVm
    {
        public List<MovieCardVm> Featured { get; set; } = new List<MovieCardVm>();
        public List<MovieCardVm> NowShowing { get; set; } = new List<MovieCardVm>();

        // Thông tin coupon
        public int? UserPoints { get; set; }
        public List<CouponVm> AvailableCoupons { get; set; } = new List<CouponVm>();

        // Phân trang cho NowShowing (nếu muốn giữ lại)
        public int NowShowingPageIndex { get; set; } = 1;
        public int NowShowingPageSize { get; set; } = 100;
        public int NowShowingTotalItems { get; set; } = 0;
        public int NowShowingTotalPages =>
            NowShowingPageSize <= 0
                ? 0
                : (int)Math.Ceiling(NowShowingTotalItems / (double)NowShowingPageSize);

        // Thanh chọn ngày (giống BookTicket)
        public List<DateTime> WeekDays { get; set; } = new List<DateTime>();
        public DateTime SelectedDate { get; set; } = DateTime.Today;
    }
}
