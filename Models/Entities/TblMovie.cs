using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblMovie
{
    public int MovieId { get; set; }

    public string MovieTitle { get; set; } = null!;

    public string MovieGenre { get; set; } = null!;

    public string MovieDirector { get; set; } = null!;

    public string MovieImg { get; set; } = null!;

    public DateTime MovieStartDate { get; set; }

    public DateTime MovieEndDate { get; set; }

    public int MovieRate { get; set; }

    public int MovieDurationMin { get; set; }

    public string? MovieDescription { get; set; }

    public int? MovieStatus { get; set; }

    public virtual ICollection<TblShowtime> TblShowtimes { get; set; } = new List<TblShowtime>();
}
