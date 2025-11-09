using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblSeat
{
    public int SeatId { get; set; }

    public int SeatScreenId { get; set; }

    public string SeatLabel { get; set; } = null!;

    public string SeatRow { get; set; } = null!;

    public int SeatCol { get; set; }

    public bool? SeatIsActive { get; set; }

    public virtual TblScreen SeatScreen { get; set; } = null!;

    public virtual ICollection<TblShowtimeSeat> TblShowtimeSeats { get; set; } = new List<TblShowtimeSeat>();
}
