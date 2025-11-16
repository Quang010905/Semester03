using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblParkingSpot
{
    public int ParkingSpotId { get; set; }

    public int SpotLevelId { get; set; }

    public string SpotCode { get; set; } = null!;

    public string SpotRow { get; set; } = null!;

    public int SpotCol { get; set; }

    public int SpotStatus { get; set; }

    public virtual TblParkingLevel SpotLevel { get; set; } = null!;
}
