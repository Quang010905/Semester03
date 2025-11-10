using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblShowtime
{
    public int ShowtimeId { get; set; }

    public int ShowtimeScreenId { get; set; }

    public int ShowtimeMovieId { get; set; }

    public DateTime ShowtimeStart { get; set; }

    public decimal ShowtimePrice { get; set; }

    public virtual TblMovie ShowtimeMovie { get; set; } = null!;

    public virtual TblScreen ShowtimeScreen { get; set; } = null!;

    public virtual ICollection<TblShowtimeSeat> TblShowtimeSeats { get; set; } = new List<TblShowtimeSeat>();
}
