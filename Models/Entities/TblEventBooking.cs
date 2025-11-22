using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblEventBooking
{
    public int EventBookingId { get; set; }

    public int EventBookingTenantPositionId { get; set; }

    public int EventBookingUserId { get; set; }

    public int EventBookingEventId { get; set; }

    public DateOnly? EventBookingDate { get; set; }

    public int? EventBookingQuantity { get; set; }

    public decimal? EventBookingUnitPrice { get; set; }

    public Guid? EventBookingOrderGroup { get; set; }

    public decimal? EventBookingTotalCost { get; set; }

    public int? EventBookingPaymentStatus { get; set; }

    public int? EventBookingStatus { get; set; }

    public string? EventBookingNotes { get; set; }

    public DateTime? EventBookingCreatedDate { get; set; }

    public virtual TblEvent EventBookingEvent { get; set; } = null!;

    public virtual TblTenantPosition EventBookingTenantPosition { get; set; } = null!;

    public virtual ICollection<TblEventBookingHistory> TblEventBookingHistories { get; set; } = new List<TblEventBookingHistory>();

    public virtual TblTenant EventBookingTenant { get; set; } = null!;

    public virtual TblUser EventBookingUser { get; set; } = null!;
}
