using Semester03.Models.Entities;
using Semester03.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Partner.Models;
using DocumentFormat.OpenXml.Vml.Office;
using System.Globalization;
using System.Text;

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
        //lay danh sach promotion theo tenant
        public async Task<List<TenantPromotion>> GetAllPromotionsByTenantId(int tenantId)
        {
            return await _context.TblTenantPromotions
                .Where(x => x.TenantPromotionTenantId == tenantId)
                .Select(x => new TenantPromotion
                {
                    ID = x.TenantPromotionId,
                    TenantId = x.TenantPromotionTenantId,
                    Title = x.TenantPromotionTitle,
                    Img = x.TenantPromotionImg,
                    Description = x.TenantPromotionDescription,
                    DiscountPercent = x.TenantPromotionDiscountPercent ?? 0,
                    DiscountAmount = x.TenantPromotionDiscountAmount ?? 0,
                    MinBillAmount = x.TenantPromotionMinBillAmount ?? 0,
                    Start = x.TenantPromotionStart,
                    End = x.TenantPromotionEnd,
                    Status = x.TenantPromotionStatus ?? 0
                })
                .ToListAsync();
        }
        public async Task<TenantPromotion?> FindById(int id)
        {
            return await _context.TblTenantPromotions
                .Where(t => t.TenantPromotionId == id)
                .Select(x => new TenantPromotion
                {
                    ID = x.TenantPromotionId,
                    TenantId = x.TenantPromotionTenantId,
                    Title = x.TenantPromotionTitle,
                    Img = x.TenantPromotionImg,
                    Description = x.TenantPromotionDescription,
                    DiscountPercent = x.TenantPromotionDiscountPercent ?? 0,
                    DiscountAmount = x.TenantPromotionDiscountAmount ?? 0,
                    MinBillAmount = x.TenantPromotionMinBillAmount ?? 0,
                    Start = x.TenantPromotionStart,
                    End = x.TenantPromotionEnd,
                    Status = x.TenantPromotionStatus ?? 0
                })
                .FirstOrDefaultAsync();
        }
        //them
        public async Task AddPromotion(TenantPromotion entity)
        {
            try
            {
                var item = new TblTenantPromotion
                {
                    TenantPromotionTenantId = entity.TenantId,
                    TenantPromotionTitle = entity.Title,
                    TenantPromotionImg = entity.Img,
                    TenantPromotionDescription = entity.Description,
                    TenantPromotionDiscountPercent = entity.DiscountPercent,
                    TenantPromotionDiscountAmount = entity.DiscountAmount,
                    TenantPromotionMinBillAmount = entity.MinBillAmount,
                    TenantPromotionStart = entity.Start,
                    TenantPromotionEnd = entity.End,
                    TenantPromotionStatus = entity.Status
                };
                _context.TblTenantPromotions.Add(item);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }
        //Xóa promotion
        public async Task<bool> DeletePromotion(int Id)
        {
            try
            {
                var item = await _context.TblTenantPromotions.FirstOrDefaultAsync(t => t.TenantPromotionId == Id);
                if (item != null)
                {
                    _context.TblTenantPromotions.Remove(item);
                    return await _context.SaveChangesAsync() > 0;
                }
                return false;
            }
            catch (Exception)
            {

                throw;
            }
        }
        //Update promotion
        public async Task<bool> UpdatePromotion(TenantPromotion entity)
        {
            var q = await _context.TblTenantPromotions.FirstOrDefaultAsync(t => t.TenantPromotionId == entity.ID);
            if (q != null)
            {
                q.TenantPromotionTenantId = entity.TenantId;
                q.TenantPromotionTitle = entity.Title;
                q.TenantPromotionImg = entity.Img;
                q.TenantPromotionDescription = entity.Description;
                q.TenantPromotionDiscountPercent = entity.DiscountPercent;
                q.TenantPromotionDiscountAmount = entity.DiscountAmount;
                q.TenantPromotionMinBillAmount = entity.MinBillAmount;
                q.TenantPromotionStart = entity.Start;
                q.TenantPromotionEnd = entity.End;
                q.TenantPromotionStatus = entity.Status;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        public async Task<bool> UpdatePromotionStatus(TenantPromotion entity)
        {
            var q = await _context.TblTenantPromotions.FirstOrDefaultAsync(t => t.TenantPromotionId == entity.ID);
            if (q != null)
            {
                q.TenantPromotionStatus = entity.Status;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> CheckPromotionAsync(string name, int tenantId, int? excludeId = null)
        {
            string normalizedInput = NormalizeName(name);

            var allPromotionNames = await _context.TblTenantPromotions
                .Where(t => t.TenantPromotionTenantId == tenantId &&
                (!excludeId.HasValue || t.TenantPromotionId != excludeId.Value))
                .Select(t => t.TenantPromotionTitle)
                .ToListAsync();

            return allPromotionNames.Any(dbName => NormalizeName(dbName) == normalizedInput);
        }

        private string NormalizeName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Bỏ khoảng trắng và chuyển về chữ thường
            return new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
        }

        public string NormalizeSearch(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string lower = input.ToLowerInvariant();
            string normalized = lower.Normalize(NormalizationForm.FormD);

            StringBuilder sb = new StringBuilder();
            foreach (char c in normalized)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return new string(sb.ToString()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());
        }
    }
}
