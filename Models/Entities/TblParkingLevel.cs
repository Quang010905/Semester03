using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblParkingLevel
{
    public int LevelId { get; set; }

    public string LevelName { get; set; } = null!;

    public int LevelCapacity { get; set; }

    public virtual ICollection<TblParkingSpot> TblParkingSpots { get; set; } = new List<TblParkingSpot>();
}
