using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities
{
    public partial class TblShowtimeSeat
    {
        public TblShowtimeSeat()
        {
            TblTickets = new HashSet<TblTicket>();
        }

        public int ShowtimeSeatId { get; set; }
        public int ShowtimeSeatShowtimeId { get; set; }
        public int ShowtimeSeatSeatId { get; set; }
        public string? ShowtimeSeatStatus { get; set; }
        public int ShowtimeSeatReservedByUserId { get; set; }
        public DateTime ShowtimeSeatReservedAt { get; set; }
        public DateTime? ShowtimeSeatUpdatedAt { get; set; }

        // navigation
        public virtual TblShowtime? ShowtimeSeatShowtime { get; set; }
        public virtual TblSeat? ShowtimeSeatSeat { get; set; }
        public virtual TblUser? ShowtimeSeatReservedByUser { get; set; }

        public virtual ICollection<TblTicket> TblTickets { get; set; } // mapped in context .WithMany(p => p.TblTickets)
    }
}
