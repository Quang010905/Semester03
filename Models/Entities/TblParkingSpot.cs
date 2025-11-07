using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblParkingSpot
{
    public int ParkingSpotId { get; set; }

    public string? ParkingSpotCode { get; set; }

    public string? ParkingSpotLevel { get; set; }

    public int? ParkingSpotStatus { get; set; }

    public string? ParkingSpotFloor { get; set; }
}
