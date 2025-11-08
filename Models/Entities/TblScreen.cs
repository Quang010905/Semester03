using System.Collections.Generic;


namespace Semester03.Models.Entities
{
    public partial class TblScreen
    {
        public TblScreen()
        {
            TblSeats = new HashSet<TblSeat>();
            TblShowtimes = new HashSet<TblShowtime>();
        }


        public int ScreenId { get; set; }
        public int ScreenCinemaId { get; set; }
        public string ScreenName { get; set; }
        public int ScreenSeats { get; set; }


        public virtual TblCinema ScreenCinema { get; set; }
        public virtual ICollection<TblSeat> TblSeats { get; set; }
        public virtual ICollection<TblShowtime> TblShowtimes { get; set; }
    }
}