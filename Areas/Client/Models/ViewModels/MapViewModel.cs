using System;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Models.ViewModels
{
    /// <summary>
    /// View model chính cho bản đồ mall (floor map).
    /// Giữ lại tất cả các cột cũ bạn gửi và bổ sung một số trường tiện ích.
    /// </summary>
    public class MapViewModel
    {
        public MapViewModel()
        {
            AvailableFloors = new List<int>() { 0, 1, 2, 3 };
            Positions = new List<TenantPositionDto>();
        }

        // Current selected floor
        public int FloorNumber { get; set; }

        // Optional image path for the floor (relative under wwwroot)
        public string FloorImagePath { get; set; }

        // Floor / positions sizes
        public decimal FloorAreaM2 { get; set; }
        public decimal ReservedAreaM2 { get; set; }
        public decimal PositionAreaM2 { get; set; }

        // Layout settings
        public int Columns { get; set; } = 8;

        // Existing fields you had for capacity/render info
        public int MaxPositionsComputed { get; set; }
        public int RenderedCellCount { get; set; }

        // Floors available and list of positions
        public List<int> AvailableFloors { get; set; }

        public List<TenantPositionDto> Positions { get; set; }

        // Additional fields useful for filtering / UI state (kept to avoid losing columns)
        public string CurrentSearch { get; set; }
        public int? CurrentStatus { get; set; }     // 0 = vacant, 1 = occupied
        public int? CurrentFloor { get; set; }

        // Paging / meta (optional)
        public int TotalPositions => Positions?.Count ?? 0;
    }

    /// <summary>
    /// Tenant DTO - giữ nguyên tên cột bạn cung cấp
    /// </summary>
    public class TenantDto
    {
        public TenantDto() { }

        public int Tenant_Id { get; set; }
        public string Tenant_Name { get; set; }
        public string Tenant_Img { get; set; }
        public string Tenant_UserID { get; set; }
        public int? Tenant_Status { get; set; }

        // Extra optional metadata (không xóa để tránh mất cột)
        public string Tenant_Description { get; set; }
        public string Tenant_Phone { get; set; }
        public string Tenant_Email { get; set; }
        public string Tenant_ContactPerson { get; set; }
    }

    /// <summary>
    /// TenantPosition DTO - giữ nguyên tất cả cột bạn đã định nghĩa,
    /// bổ sung một vài trường trợ giúp (OrderIndex, Notes) nhưng không xóa gì.
    /// </summary>
    public class TenantPositionDto
    {
        public TenantPositionDto() { }

        public int TenantPosition_ID { get; set; }
        public string TenantPosition_Location { get; set; }

        // assigned tenant id (nullable)
        public int? TenantPosition_AssignedTenantID { get; set; }

        // existing area and floor fields
        public decimal TenantPosition_Area_M2 { get; set; }
        public int TenantPosition_Floor { get; set; }

        // status: 0=vacant,1=occupied
        public int TenantPosition_Status { get; set; }

        // coordinates existing in DB (nullable)
        public decimal? TenantPosition_LeftPct { get; set; }
        public decimal? TenantPosition_TopPct { get; set; }

        // reference to tenant object (may be null if vacant)
        public TenantDto Tenant { get; set; }

        // Optional extra metadata (kept to avoid losing previous DB columns)
        public string TenantPosition_Description { get; set; }
        public decimal? TenantPosition_Rent_Price_Per_M2 { get; set; }
        public string TenantPosition_Tag { get; set; }

        // Helpers used by UI/layout (computed, not persisted)
        public int? OrderIndex { get; set; }
        public string DisplayLabel => string.IsNullOrWhiteSpace(TenantPosition_Location) ? $"#{TenantPosition_ID}" : TenantPosition_Location;
        public string ShortName => Tenant?.Tenant_Name ?? (TenantPosition_AssignedTenantID.HasValue ? $"T-{TenantPosition_AssignedTenantID}" : "VACANT");
    }
}
