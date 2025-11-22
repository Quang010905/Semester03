using Semester03.Models.Entities;
using Semester03.Models.ViewModels;

public class TenantPromotionListVm
{
    public int? SelectedTenantId { get; set; }
    public IEnumerable<TblTenant> Tenants { get; set; } = new List<TblTenant>();

    public IEnumerable<TenantPromotionVm> ActivePromotions { get; set; } = new List<TenantPromotionVm>();
    public IEnumerable<TenantPromotionVm> ExpiredPromotions { get; set; } = new List<TenantPromotionVm>();

    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
