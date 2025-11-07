using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblTicket
{
    public int TicketId { get; set; }

    public int TicketShowtimeId { get; set; }

    public string? TicketSeat { get; set; }

    public int? TicketBuyerUserId { get; set; }

    public string? TicketStatus { get; set; }

    public decimal? TicketPrice { get; set; }

    public DateTime? TicketPurchasedAt { get; set; }

    public virtual TblUser? TicketBuyerUser { get; set; }

    public virtual TblShowtime TicketShowtime { get; set; } = null!;
}
