using Semester03.Models.Entities;
using Semester03.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Semester03.Models.Repositories
{
    public class TenantPromotionRepository
    {
        private readonly AbcdmallContext _context;

        public TenantPromotionRepository(AbcdmallContext context)
        {
            _context = context;
        }

        // Lấy top N promotions active
        public List<TenantPromotionVm> GetTopLatestPromotions(int top = 6)
        {
            var today = DateTime.UtcNow;

            return _context.TblTenantPromotions
                .Where(p => p.TenantPromotionEnd >= today)
                .OrderByDescending(p => p.TenantPromotionStart)
                .Take(top)
                .Select(p => new TenantPromotionVm
                {
                    Id = p.TenantPromotionId,
                    Title = p.TenantPromotionTitle,
                    Img = p.TenantPromotionImg,
                    DiscountAmount = p.TenantPromotionDiscountAmount,
                    DiscountPercent = p.TenantPromotionDiscountPercent,
                    PromotionStart = p.TenantPromotionStart,
                    PromotionEnd = p.TenantPromotionEnd
                }).ToList();
        }

        // Lấy danh sách phân trang và active/expired
        public TenantPromotionListVm GetPromotions(int? tenantId, int page = 1, int pageSize = 8)
        {
            var query = _context.TblTenantPromotions
                .Include(x => x.TenantPromotionTenant)
                .OrderByDescending(x => x.TenantPromotionStart)
                .AsQueryable();

            if (tenantId.HasValue)
                query = query.Where(x => x.TenantPromotionTenantId == tenantId);

            var today = DateTime.UtcNow;

            var activeList = query
                .Where(p => p.TenantPromotionEnd >= today)
                .Select(p => new TenantPromotionVm
                {
                    Id = p.TenantPromotionId,
                    Title = p.TenantPromotionTitle,
                    Img = p.TenantPromotionImg,
                    PromotionStart = p.TenantPromotionStart,
                    PromotionEnd = p.TenantPromotionEnd,
                    DiscountAmount = p.TenantPromotionDiscountAmount,
                    DiscountPercent = p.TenantPromotionDiscountPercent
                });

            int totalItems = activeList.Count();
            var items = activeList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var expired = query
                .Where(p => p.TenantPromotionEnd < today)
                .OrderByDescending(p => p.TenantPromotionEnd)
                .Select(p => new TenantPromotionVm
                {
                    Id = p.TenantPromotionId,
                    Title = p.TenantPromotionTitle,
                    Img = p.TenantPromotionImg,
                    PromotionEnd = p.TenantPromotionEnd
                }).ToList();

            return new TenantPromotionListVm
            {
                SelectedTenantId = tenantId,
                Tenants = _context.TblTenants.ToList(),
                ActivePromotions = items,
                ExpiredPromotions = expired,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }

        // Lấy chi tiết promotion
        public TenantPromotionVm GetPromotionDetail(int id)
        {
            var p = _context.TblTenantPromotions
                .Include(x => x.TenantPromotionTenant)
                .FirstOrDefault(x => x.TenantPromotionId == id);

            if (p == null) return null;

            return new TenantPromotionVm
            {
                Id = p.TenantPromotionId,
                Title = p.TenantPromotionTitle,
                Img = p.TenantPromotionImg,
                Description = p.TenantPromotionDescription,
                PromotionStart = p.TenantPromotionStart,
                PromotionEnd = p.TenantPromotionEnd,
                DiscountAmount = p.TenantPromotionDiscountAmount,
                DiscountPercent = p.TenantPromotionDiscountPercent,
                MinBillAmount = p.TenantPromotionMinBillAmount
            };
        }
    }
}
