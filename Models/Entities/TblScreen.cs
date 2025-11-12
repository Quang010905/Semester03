using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblScreen
{
    public int ScreenId { get; set; }

    public int ScreenCinemaId { get; set; }

    public string ScreenName { get; set; } = null!;

    public int ScreenSeats { get; set; }

    public virtual TblCinema ScreenCinema { get; set; } = null!;

    public virtual ICollection<TblSeat> TblSeats { get; set; } = new List<TblSeat>();

    public virtual ICollection<TblShowtime> TblShowtimes { get; set; } = new List<TblShowtime>();
}
