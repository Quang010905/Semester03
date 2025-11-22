namespace Semester03.Areas.Client.Models.ViewModels
{
    public class CouponVm
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal DiscountPercent { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public int? MinimumPointsRequired { get; set; }
        public bool AlreadyOwned { get; set; }
    }

}
