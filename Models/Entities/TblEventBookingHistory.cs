using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblEventBookingHistory
{
    public long EventBookingHistoryId { get; set; }

    public int? EventBookingHistoryBookingId { get; set; }

    public int? EventBookingHistoryEventId { get; set; }

    public int? EventBookingHistoryUserId { get; set; }

    public string EventBookingHistoryAction { get; set; } = null!;

    public string? EventBookingHistoryDetails { get; set; }

    public DateOnly? EventBookingHistoryRelatedDate { get; set; }

    public int? EventBookingHistoryQuantity { get; set; }

    public DateTime? EventBookingHistoryCreatedAt { get; set; }

    public virtual TblEventBooking? EventBookingHistoryBooking { get; set; }

    public virtual TblEvent? EventBookingHistoryEvent { get; set; }
}
