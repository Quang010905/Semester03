using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Partner.Models;
using Semester03.Models.Entities;
using Semester03.Models.ViewModels;
using System.Globalization;
using System.Text;

namespace Semester03.Models.Repositories
{
    public class ProductRepository
    {
        private readonly AbcdmallContext _context;

        public ProductRepository(AbcdmallContext context)
        {
            _context = context;
        }
        //lay san pham theo danh sach cua category 
        public async Task<List<Product>> GetAllProductsByCateId(int cateId)
        {
            return await _context.TblProducts
                .Where(x => x.ProductCategoryId == cateId)
                .Select(x => new Product
                {
                    Id = x.ProductId,
                    Name = x.ProductName,
                    Status = x.ProductStatus ?? 0,
                    Img = x.ProductImg,
                    CateId = cateId,
                    Description = x.ProductDescription,
                    Price = x.ProductPrice,
                    CreatedAt = (DateTime)x.ProductCreatedAt
                })
                .ToListAsync();
        }

        //them
        public async Task AddProduct(Product entity)
        {
            try
            {
                var item = new TblProduct
                {
                    ProductName = entity.Name,
                    ProductImg = entity.Img,
                    ProductCategoryId = entity.CateId,
                    ProductDescription = entity.Description,
                    ProductPrice = entity.Price,
                    ProductStatus = entity.Status,
                    ProductCreatedAt = DateTime.Now
                };
                _context.TblProducts.Add(item);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }
        //Xóa category
        public async Task<bool> DeleteProduct(int Id)
        {
            try
            {
                var item = await _context.TblProducts.FirstOrDefaultAsync(t => t.ProductId == Id);
                if (item != null)
                {
                    _context.TblProducts.Remove(item);
                    return await _context.SaveChangesAsync() > 0;
                }
                return false;
            }
            catch (Exception)
            {

                return false;
            }
        }
        //Update product
        public async Task<bool> UpdateProduct(Product entity)
        {
            var q = await _context.TblProducts.FirstOrDefaultAsync(t => t.ProductId == entity.Id);
            if (q != null)
            {
                q.ProductId = entity.Id;
                q.ProductName = entity.Name;
                q.ProductImg = entity.Img;
                q.ProductCategoryId = entity.CateId;
                q.ProductDescription = entity.Description;
                q.ProductPrice = entity.Price;
                q.ProductStatus = entity.Status;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        //lay thong tin chi tiet tenant
        public async Task<Product?> FindById(int id)
        {
            return await _context.TblProducts
                .Where(t => t.ProductId == id)
                .Select(t => new Product
                {
                    Id = t.ProductId,
                    Name = t.ProductName,
                    Status = t.ProductStatus ?? 0,
                    Img = t.ProductImg,
                    Description = t.ProductDescription,
                    Price = t.ProductPrice,
                    CateId = t.ProductCategoryId
                })
                .FirstOrDefaultAsync();
        }
        //kiem tra trung ten
        public async Task<bool> CheckProductNameAsync(string name, int cateId, int? excludeId = null)
        {
            string normalizedInput = NormalizeName(name);

            var allProductNames = await _context.TblProducts
                .Where(t => t.ProductCategoryId == cateId &&
                (!excludeId.HasValue || t.ProductId != excludeId.Value))
                .Select(t => t.ProductName)
                .ToListAsync();

            return allProductNames.Any(dbName => NormalizeName(dbName) == normalizedInput);
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
