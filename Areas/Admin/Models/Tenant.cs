namespace Semester03.Areas.Admin.Models
{
    public class Tenant
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Image { get; set; } = "";
        public int UserId { get; set; } = 0;
        public int TypeId { get; set; } = 0;
        public string Description { get; set; } = "";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
