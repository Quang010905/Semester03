using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblCouponUser
{
    public int CouponUserId { get; set; }

    public int? CouponId { get; set; }

    public int? UsersId { get; set; }

    public virtual TblCoupon? Coupon { get; set; }

    public virtual TblUser? Users { get; set; }
}
