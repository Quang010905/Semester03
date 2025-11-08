using System.Collections.Generic;


namespace Semester03.Models.Entities
{
    public partial class TblSeat
    {
        public TblSeat()
        {
            TblShowtimeSeats = new HashSet<TblShowtimeSeat>();
        }


        public int SeatId { get; set; }
        public int SeatScreenId { get; set; }
        public string SeatLabel { get; set; }
        public string SeatRow { get; set; }
        public int SeatCol { get; set; }
        public bool? SeatIsActive { get; set; }


        public virtual TblScreen SeatScreen { get; set; }
        public virtual ICollection<TblShowtimeSeat> TblShowtimeSeats { get; set; }
    }
}