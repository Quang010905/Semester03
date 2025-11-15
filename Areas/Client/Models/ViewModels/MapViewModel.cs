using System.Collections.Generic;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class MapViewModel
    {
        public int FloorNumber { get; set; }
        public string FloorImagePath { get; set; } // relative path under wwwroot
        public decimal FloorAreaM2 { get; set; }
        public decimal ReservedAreaM2 { get; set; }
        public decimal PositionAreaM2 { get; set; }

        public int Columns { get; set; } = 8;

        public int MaxPositionsComputed { get; set; }
        public int RenderedCellCount { get; set; }

        public List<int> AvailableFloors { get; set; } = new List<int>() { 0, 1, 2, 3 };

        public List<TenantPositionDto> Positions { get; set; } = new List<TenantPositionDto>();
    }

    public class TenantPositionDto
    {
        public int TenantPosition_ID { get; set; }
        public string TenantPosition_Location { get; set; }
        public int? TenantPosition_AssignedTenantID { get; set; }
        public decimal TenantPosition_Area_M2 { get; set; }
        public int TenantPosition_Floor { get; set; }
        public int TenantPosition_Status { get; set; }

        // new: coordinates from DB
        public decimal? TenantPosition_LeftPct { get; set; }
        public decimal? TenantPosition_TopPct { get; set; }
    }
}
