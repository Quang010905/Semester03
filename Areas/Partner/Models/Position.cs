namespace Semester03.Areas.Partner.Models
{
    public class Position
    {
        public int Id { get; set; } = 0;
        public string Location { get; set; } = "";
        public int Floor { get; set; } = 0;
        public decimal Area { get; set; } = 0;
        public decimal PricePerM2 { get; set; } = 0;
        public int Status { get; set; } = 0;
        public int AssignedTenantId {  get; set; } = 0;
        public DateTime Start;
        public DateTime End;
    }
}
