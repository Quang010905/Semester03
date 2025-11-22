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
    }
}
