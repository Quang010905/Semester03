namespace Semester03.Areas.Client.Models.ViewModels
{
    public class CouponDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal DiscountPercent { get; set; }
        public int? MinimumPointsRequired { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsActive { get; set; }
    }

}
