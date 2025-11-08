using System;

namespace Semester03.Models.Entities
{
    public partial class TblTicket
    {
        public int TicketId { get; set; }

        // matches your Db schema / context mapping
        public int TicketShowtimeSeatId { get; set; }     // <-- fixes TicketShowtimeSeatId missing
        public int TicketBuyerUserId { get; set; }
        public string? TicketStatus { get; set; }
        public decimal TicketPrice { get; set; }
        public DateTime TicketCreatedAt { get; set; }     // <-- fixes TicketCreatedAt missing
        public DateTime TicketUpdatedAt { get; set; }

        // navigation props
        public virtual TblUser? TicketBuyerUser { get; set; }
        public virtual TblShowtimeSeat? TicketShowtimeSeat { get; set; } // <-- fixes TicketShowtimeSeat missing
    }
}
