using System;
using System.Collections.Generic;


namespace Semester03.Models.Entities
{
    public partial class TblCoupon
    {
        public TblCoupon()
        {
            TblCouponUsers = new HashSet<TblCouponUser>();
        }


        public int CouponId { get; set; }
        public string CouponName { get; set; }
        public string CouponDescription { get; set; }
        public decimal CouponDiscountPercent { get; set; }
        public DateTime CouponValidFrom { get; set; }
        public DateTime CouponValidTo { get; set; }
        public bool? CouponIsActive { get; set; }


        public virtual ICollection<TblCouponUser> TblCouponUsers { get; set; }
    }
}