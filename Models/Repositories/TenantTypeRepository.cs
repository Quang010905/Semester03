using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Admin.Models;
using Semester03.Models.Entities;

namespace Semester03.Models.Repositories
{
    /// <summary>
    /// Repository quản lý loại Tenant (TenantType) — dùng DI thay vì singleton.
    /// </summary>
    public class TenantTypeRepository
    {
        private readonly AbcdmallContext _context;

        // ✅ Inject DbContext qua constructor
        public TenantTypeRepository(AbcdmallContext context)
        {
            _context = context;
        }

        public async Task<TenantType?> FindByIdAsync(int id)
        {
            return await _context.TblTenantTypes
                .Where(t => t.TenantTypeId == id)
                .Select(t => new TenantType
                {
                    Id = t.TenantTypeId,
                    Name = t.TenantTypeName,
                    Status = t.TenantTypeStatus ?? 0
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<TenantType>> GetAllAsync()
        {
            return await _context.TblTenantTypes
                .Select(x => new TenantType
                {
                    Id = x.TenantTypeId,
                    Name = x.TenantTypeName,
                    Status = x.TenantTypeStatus ?? 0
                })
                .ToListAsync();
        }

        public async Task<List<TenantType>> GetActiveTenantType()
        {
            return await _context.TblTenantTypes
                .Select(x => new TenantType
                {
                    Id = x.TenantTypeId,
                    Name = x.TenantTypeName,
                    Status = x.TenantTypeStatus ?? 0
                }).Where(x => x.Status == 1)
                .ToListAsync();
        }

        public async Task AddAsync(TenantType entity)
        {
            var item = new TblTenantType
            {
                TenantTypeName = entity.Name,
                TenantTypeStatus = entity.Status
            };

            _context.TblTenantTypes.Add(item);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var item = await _context.TblTenantTypes
                    .FirstOrDefaultAsync(t => t.TenantTypeId == id);

                if (item == null) return false;

                _context.TblTenantTypes.Remove(item);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                // Lỗi khóa ngoại
                return false;
            }
        }


        public async Task<bool> UpdateAsync(TenantType entity)
        {
            var q = await _context.TblTenantTypes.FirstOrDefaultAsync(t => t.TenantTypeId == entity.Id);
            if (q != null)
            {
                q.TenantTypeName = entity.Name;
                q.TenantTypeStatus = entity.Status;
                return await _context.SaveChangesAsync() > 0;
            }
            return false;
        }

        public async Task<bool> CheckTenantTypeNameAsync(string name, int? excludeId = null)
        {
            string normalizedInput = NormalizeName(name);

            var allTenantTypeNames = await _context.TblTenantTypes
                .Where(t => !excludeId.HasValue || t.TenantTypeId != excludeId.Value)
                .Select(t => t.TenantTypeName)
                .ToListAsync();

            return allTenantTypeNames.Any(dbName => NormalizeName(dbName) == normalizedInput);
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

        public async Task<TenantType?> GetTenantTypeByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            return await _context.TblTenantTypes
                .Where(t => t.TenantTypeName == name)
                .Select(x => new TenantType
                {
                    Id = x.TenantTypeId,
                    Name = x.TenantTypeName,
                    Status = x.TenantTypeStatus ?? 0
                })
                .FirstOrDefaultAsync();
        }
    }
}
