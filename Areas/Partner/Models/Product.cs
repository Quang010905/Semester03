namespace Semester03.Areas.Partner.Models
{
    public class Product
    {
        public int Id { get; set; } = 0;
        public string Img { get; set; } = "";
        public int CateId { get; set; } = 0;
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; } = 0;
        public int Status { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
