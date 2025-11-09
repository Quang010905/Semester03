using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblCinema
{
    public int CinemaId { get; set; }

    public string CinemaName { get; set; } = null!;

    public string? CinemaImg { get; set; }

    public string? CinemaDescription { get; set; }

    public virtual ICollection<TblScreen> TblScreens { get; set; } = new List<TblScreen>();
}
