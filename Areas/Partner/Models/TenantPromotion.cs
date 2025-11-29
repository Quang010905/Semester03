namespace Semester03.Areas.Partner.Models
{
    public class TenantPromotion
    {
        public int ID { get; set; } = 0;
        public int TenantId { get; set; } = 0;
        public string Title { get; set; } = "";
        public string Img { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal DiscountPercent { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal MinBillAmount {  get; set; } = 0;
        public DateTime Start {  get; set; } = DateTime.Now;
        public DateTime End { get; set; }
        public int Status { get; set; } = 1;
    }
}
