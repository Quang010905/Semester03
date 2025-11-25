namespace Semester03.Areas.Client.Models.ViewModels
{
    public class FeaturedStoreViewModel
    {
        public int TenantId { get; set; }
        public string TenantName { get; set; } = "";
        public string TenantImg { get; set; } = "";
        public string TenantDescription { get; set; } = "";
        public string TenantTypeName { get; set; } = "";
        public string Position { get; set; } = "";
        public double Rating { get; set; } // thêm dòng này

    }
}
