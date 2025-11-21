namespace Semester03.Areas.Partner.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Image { get; set; } = "";
        public int Status { get; set; } = 0;
        public int TenantId { get; set; } = 0;
    }
}
