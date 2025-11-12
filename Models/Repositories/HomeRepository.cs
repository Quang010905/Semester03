using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using Semester03.Models.ViewModels; // for FeaturedStoreViewModel

namespace Semester03.Areas.Client.Repositories
{
    public class HomeRepository
    {
        private readonly AbcdmallContext _db;
        public HomeRepository(AbcdmallContext db) => _db = db;

        /// <summary>
        /// Lấy các sự kiện sắp tới (active) để hiển thị trên home
        /// </summary>
        public async Task<List<EventCardVm>> GetUpcomingEventsAsync(int take = 8)
        {
            var now = DateTime.UtcNow;

            var q = _db.TblEvents
                .AsNoTracking()
                .Where(e => e.EventStatus == 1 && e.EventEnd >= now)
                .OrderBy(e => e.EventStart)
                .Select(e => new EventCardVm
                {
                    Id = e.EventId,
                    Title = e.EventName,
                    ShortDescription = e.EventDescription.Length > 180 ? e.EventDescription.Substring(0, 180) + "..." : e.EventDescription,
                    StartDate = e.EventStart,
                    EndDate = e.EventEnd,
                    ImageUrl = string.IsNullOrWhiteSpace(e.EventImg) ? "/images/event-placeholder.png" : (e.EventImg.StartsWith("/") ? e.EventImg : $"/images/events/{e.EventImg}"),
                    MaxSlot = e.EventMaxSlot,
                    Status = e.EventStatus ?? 0,
                    TenantPositionId = e.EventTenantPositionId
                });

            return await q.Take(take).ToListAsync();
        }

        /// <summary>
        /// Lấy danh sách cửa hàng nổi bật (tenant) để hiển thị trên home
        /// </summary>
        public async Task<List<FeaturedStoreViewModel>> GetFeaturedStoresAsync(int take = 6)
        {
            // Left join tenant -> tenantType and tenantPosition (optional)
            var q = from t in _db.TblTenants.AsNoTracking()
                    join tt in _db.TblTenantTypes.AsNoTracking() on t.TenantTypeId equals tt.TenantTypeId into ttt
                    from tt in ttt.DefaultIfEmpty()
                    join tp in _db.TblTenantPositions.AsNoTracking() on t.TenantId equals tp.TenantPositionAssignedTenantId into tpp
                    from tp in tpp.DefaultIfEmpty()
                    orderby t.TenantCreatedAt descending
                    select new FeaturedStoreViewModel
                    {
                        TenantId = t.TenantId,
                        TenantName = t.TenantName,
                        TenantImg = string.IsNullOrWhiteSpace(t.TenantImg) ? "/Admin/img/store-placeholder.png" : (t.TenantImg.StartsWith("/") ? t.TenantImg : $"/Admin/img/{t.TenantImg}"),
                        TenantDescription = t.TenantDescription,
                        TenantTypeName = tt != null ? tt.TenantTypeName : "",
                        Position = tp != null ? tp.TenantPositionLocation : ""
                    };

            return await q.Take(take).ToListAsync();
        }

        /// <summary>
        /// Optional: load promotions/messages (simple string list) from Notification/Coupon or ViewData
        /// Example: return top active coupon names
        /// </summary>
        public async Task<List<string>> GetPromotionsAsync(int take = 5)
        {
            // Simple sample: use Coupon names (active)
            var q = _db.TblCoupons
                .AsNoTracking()
                .Where(c => c.CouponIsActive == true)
                .OrderByDescending(c => c.CouponValidFrom)
                .Select(c => c.CouponName);

            return await q.Take(take).ToListAsync();
        }
    }
}
