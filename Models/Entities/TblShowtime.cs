using System.Collections.Generic;
using System;


namespace Semester03.Models.Entities
{
    public partial class TblShowtime
    {
        public TblShowtime()
        {
            TblShowtimeSeats = new HashSet<TblShowtimeSeat>();
            TblTickets = new HashSet<TblTicket>();
        }


        public int ShowtimeId { get; set; }
        public int ShowtimeScreenId { get; set; }
        public int ShowtimeMovieId { get; set; }
        public DateTime ShowtimeStart { get; set; }
        public decimal ShowtimePrice { get; set; }


        public virtual TblMovie ShowtimeMovie { get; set; }
        public virtual TblScreen ShowtimeScreen { get; set; }
        public virtual ICollection<TblShowtimeSeat> TblShowtimeSeats { get; set; }
        public virtual ICollection<TblTicket> TblTickets { get; set; }
    }
}