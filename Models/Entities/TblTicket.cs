using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblTicket
{
    public int TicketId { get; set; }

    public int TicketShowtimeSeatId { get; set; }

    public int TicketBuyerUserId { get; set; }

    public string? TicketStatus { get; set; }

    public decimal TicketPrice { get; set; }

    public string? TicketQr { get; set; }

    public DateTime? TicketCreatedAt { get; set; }

    public DateTime? TicketUpdatedAt { get; set; }

    public virtual TblUser TicketBuyerUser { get; set; } = null!;

    public virtual TblShowtimeSeat TicketShowtimeSeat { get; set; } = null!;
}
