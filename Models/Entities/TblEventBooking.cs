using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblEventBooking
{
    public int EventBookingId { get; set; }

    public int EventBookingTenantId { get; set; }

    public int EventBookingUserId { get; set; }

    public int EventBookingEventId { get; set; }

    public decimal? EventBookingTotalCost { get; set; }

    public int? EventBookingPaymentStatus { get; set; }

    public string? EventBookingNotes { get; set; }

    public DateTime? EventBookingCreatedDate { get; set; }
    public virtual TblEvent EventBookingEvent { get; set; } = null!;
    public virtual TblTenant EventBookingTenant { get; set; } = null!;

    public virtual TblUser EventBookingUser { get; set; } = null!;
}
