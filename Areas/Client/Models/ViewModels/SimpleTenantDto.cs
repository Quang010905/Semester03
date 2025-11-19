namespace Semester03.Areas.Client.Models.ViewModels
{
    public class SimpleTenantDto
    {
        public int Tenant_ID { get; set; }
        public string Tenant_Name { get; set; }
        public string Tenant_Img { get; set; }
        public int? Tenant_UserID { get; set; }
        public int Tenant_Status { get; set; }
        public string Tenant_Description { get; set; }
    }

    public class PositionWithTenantDto
    {
        public int TenantPosition_ID { get; set; }
        public string TenantPosition_Location { get; set; }
        public decimal? TenantPosition_Area_M2 { get; set; }
        public int TenantPosition_Floor { get; set; }
        public int TenantPosition_Status { get; set; }
        public decimal? TenantPosition_LeftPct { get; set; }
        public decimal? TenantPosition_TopPct { get; set; }

        // assigned tenant id (nullable)
        public int? TenantPosition_AssignedTenantID { get; set; }

        // nested tenant info (null if no tenant)
        public SimpleTenantDto Tenant { get; set; }
    }
}
