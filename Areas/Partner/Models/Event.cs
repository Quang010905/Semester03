namespace Semester03.Areas.Partner.Models
{
    public class Event
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = "";
        public string Img { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Start { get; set; } = DateTime.Now;
        public DateTime End { get; set; }
        public int Status { get; set; } = 0;
        public int MaxSlot { get; set; } = 0;
        public decimal UnitPrice { get; set; } = 0;
        public int TenantPositionId { get; set; } = 0;
    }
}
