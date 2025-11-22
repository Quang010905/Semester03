using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Admin.Models;
using Semester03.Areas.Partner.Models;
using Semester03.Models.Entities;
using System.Globalization;
using System.Text;

namespace Semester03.Models.Repositories
{
    public class CategoryRepository
    {
        private readonly AbcdmallContext _context;

        public CategoryRepository(AbcdmallContext context)
        {
            _context = context;
        }
        public async Task<List<Category>> GetAllCategoriesByTenantId(int tenantId)
        {
            return await _context.TblProductCategories
                .Where(x => x.ProductCategoryTenantId == tenantId)
                .Select(x => new Category
                {
                    Id = x.ProductCategoryId,
                    Name = x.ProductCategoryName,
                    Status = x.ProductCategoryStatus ?? 0,
                    Image = x.ProductCategoryImg,
                    TenantId = x.ProductCategoryTenantId
                })
                .ToListAsync();
        }


        //them
        public async Task AddCategory(Category entity)
        {
            try
            {
                var item = new TblProductCategory
                {
                    ProductCategoryName = entity.Name,
                    ProductCategoryStatus = entity.Status,
                    ProductCategoryImg = entity.Image,
                    ProductCategoryTenantId = entity.TenantId
                };
                _context.TblProductCategories.Add(item);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }
        //Xóa category
        public async Task<bool> DeleteCategory(int Id)
        {
            try
            {
                var item = await _context.TblProductCategories.FirstOrDefaultAsync(t => t.ProductCategoryId == Id);
                if (item != null)
                {
                    _context.TblProductCategories.Remove(item);
                    return await _context.SaveChangesAsync() > 0;
                }
                return false;
            }
            catch (Exception)
            {

                return false;
            }
        }
        //Update category
        public async Task<bool> UpdateCategory(Category entity)
        {
            var q = await _context.TblProductCategories.FirstOrDefaultAsync(t => t.ProductCategoryId == entity.Id);
            if (q != null)
            {
                q.ProductCategoryName = entity.Name;
                q.ProductCategoryImg = entity.Image;
                q.ProductCategoryStatus = entity.Status;
                q.ProductCategoryTenantId = entity.TenantId;
                return await _context.SaveChangesAsync() > 0;
            }
            return false;
        }
        //lay thong tin chi tiet tenant
        public async Task<Category?> FindById(int id)
        {
            return await _context.TblProductCategories
                .Where(t => t.ProductCategoryId == id)
                .Select(t => new Category
                {
                    Id = t.ProductCategoryId,
                    Name = t.ProductCategoryName,
                    Status = t.ProductCategoryStatus ?? 0,
                    Image = t.ProductCategoryImg,
                    TenantId = t.ProductCategoryTenantId
                })
                .FirstOrDefaultAsync();
        }
        //kiem tra trung ten
        public async Task<bool> CheckCategoryNameAsync(string name, int? excludeId = null)
        {
            string normalizedInput = NormalizeName(name);

            var allCategoryNames = await _context.TblProductCategories
                .Where(t => !excludeId.HasValue || t.ProductCategoryId != excludeId.Value)
                .Select(t => t.ProductCategoryName)
                .ToListAsync();

            return allCategoryNames.Any(dbName => NormalizeName(dbName) == normalizedInput);
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
