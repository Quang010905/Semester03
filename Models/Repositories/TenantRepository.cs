using Semester03.Models.Entities;
using Semester03.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Semester03.Areas.Admin.Models;
using System.Globalization;
using System.Text;

namespace Semester03.Models.Repositories
{
    public class TenantRepository
    {
        private readonly AbcdmallContext _context;

        public TenantRepository(AbcdmallContext context)
        {
            _context = context;
        }

        // Lấy Featured Stores (top 6)
        public List<FeaturedStoreViewModel> GetFeaturedStores(int top = 6)
        {
            return _context.TblTenants
                .Include(t => t.TenantType)
                .Include(t => t.TblTenantPositions)
                .OrderByDescending(t => t.TenantCreatedAt)
                .Take(top)
                .Select(t => new FeaturedStoreViewModel
                {
                    TenantId = t.TenantId,
                    TenantName = t.TenantName,
                    TenantImg = t.TenantImg,
                    TenantDescription = t.TenantDescription ?? "",
                    TenantTypeName = t.TenantType.TenantTypeName,
                    Position = t.TblTenantPositions.FirstOrDefault() != null
                        ? $"{t.TblTenantPositions.First().TenantPositionLocation}, Floor {t.TblTenantPositions.First().TenantPositionFloor}"
                        : ""
                })
                .ToList();
        }

        // Lấy stores theo typeId và search
        public List<FeaturedStoreViewModel> GetStores(int? typeId = null, string search = "")
        {
            var query = _context.TblTenants
                .Include(t => t.TenantType)
                .Include(t => t.TblTenantPositions)
                .AsQueryable();

            if (typeId.HasValue)
                query = query.Where(t => t.TenantTypeId == typeId.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => t.TenantName.Contains(search));

            return query.OrderBy(t => t.TenantName)
                        .Select(t => new FeaturedStoreViewModel
                        {
                            TenantId = t.TenantId,
                            TenantName = t.TenantName,
                            TenantImg = t.TenantImg,
                            TenantTypeName = t.TenantType.TenantTypeName,
                            TenantDescription = t.TenantDescription ?? "",
                            Position = t.TblTenantPositions.FirstOrDefault() != null
                                ? $"{t.TblTenantPositions.First().TenantPositionLocation}, Floor {t.TblTenantPositions.First().TenantPositionFloor}"
                                : ""
                        })
                        .ToList();
        }

        public TenantDetailsViewModel? GetTenantDetails(int tenantId)
        {
            var tenant = _context.TblTenants
                .Include(t => t.TenantType)
                .Include(t => t.TblTenantPositions)
                .FirstOrDefault(t => t.TenantId == tenantId);

            if (tenant == null) return null;

            // Lấy bình luận tenant (status = 1) – Movie/Event để null
            var comments = _context.TblCustomerComplaints
                .Include(c => c.CustomerComplaintCustomerUser)
                .Where(c => c.CustomerComplaintTenantId == tenantId && c.CustomerComplaintStatus == 1)
                .Select(c => new CustomerCommentVm
                {
                    UserName = c.CustomerComplaintCustomerUser.UsersFullName,
                    Text = c.CustomerComplaintDescription ?? "",
                    Rate = c.CustomerComplaintRate,
                    CreatedAt = c.CustomerComplaintCreatedAt
                })
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            double? avgRate = comments.Any() ? comments.Average(c => c.Rate) : null;

            return new TenantDetailsViewModel
            {
                TenantId = tenant.TenantId,
                TenantName = tenant.TenantName,
                TenantImg = tenant.TenantImg,
                TenantDescription = tenant.TenantDescription ?? "",
                TenantTypeName = tenant.TenantType.TenantTypeName,
                Position = tenant.TblTenantPositions.FirstOrDefault() != null
                    ? $"{tenant.TblTenantPositions.First().TenantPositionLocation}, Floor {tenant.TblTenantPositions.First().TenantPositionFloor}"
                    : "",
                AvgRate = avgRate,
                Comments = comments
            };
        }



        public bool AddTenantComment(int tenantId, int userId, int rate, string text)
        {
            try
            {
                var comment = new TblCustomerComplaint
                {
                    CustomerComplaintTenantId = tenantId,
                    CustomerComplaintCustomerUserId = userId,
                    CustomerComplaintRate = rate,
                    CustomerComplaintDescription = text,
                    CustomerComplaintStatus = 0, // chờ duyệt
                    CustomerComplaintCreatedAt = DateTime.Now,
                    CustomerComplaintMovieId = null,
                    CustomerComplaintEventId = null
                };

                _context.TblCustomerComplaints.Add(comment);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }


        public List<ProductCategoryVm> GetProductCategoriesByTenant(int tenantId)
        {
            return _context.TblProductCategories
                .Where(pc => pc.ProductCategoryTenantId == tenantId && (pc.ProductCategoryStatus == 1 || pc.ProductCategoryStatus == null))
                .Select(pc => new ProductCategoryVm
                {
                    Id = pc.ProductCategoryId,
                    Name = pc.ProductCategoryName,
                    Img = pc.ProductCategoryImg
                })
                .ToList();
        }

        //Lay danh sach tenant theo userid
        public async Task<List<Tenant>> GetTenantByUserId(int id)
        {
            return await _context.TblTenants
                .Select(x => new Tenant
                {
                    Id = x.TenantId,
                    Name = x.TenantName,
                    Image = x.TenantImg,
                    UserId = x.TenantUserId,    
                    TypeId = x.TenantTypeId,
                    Description = x.TenantDescription,
                    Status = x.TenantStatus ?? 0
                }).Where(x => x.UserId == id)
                .ToListAsync();
        }

        //Thêm tenant 
        public async Task AddAsync(Tenant entity)
        {
            try
            {
                var item = new TblTenant
                {
                    TenantName = entity.Name,
                    TenantImg = entity.Image,
                    TenantTypeId = entity.TypeId,
                    TenantUserId = entity.UserId,
                    TenantDescription = entity.Description,
                    TenantStatus = entity.Status,
                    TenantCreatedAt = DateTime.Now
                };
                _context.TblTenants.Add(item);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }
        //Xóa tenant
        public async Task<bool> Delete(int Id)
        {
            try
            {
                var item = await _context.TblTenants.FirstOrDefaultAsync(t => t.TenantId == Id);
                if (item != null)
                {
                    _context.TblTenants.Remove(item);
                    return await _context.SaveChangesAsync() > 0;
                }
                return false;
            }
            catch (Exception)
            {

                return false;
            }
        }
        //Update tenant
        public async Task<bool> Update(Tenant entity)
        {
            var q = await _context.TblTenants.FirstOrDefaultAsync(t => t.TenantId == entity.Id);
            if (q != null)
            {
                q.TenantName = entity.Name;
                q.TenantImg = entity.Image;
                q.TenantTypeId = entity.TypeId;
                q.TenantUserId = entity.UserId;
                q.TenantDescription = entity.Description;
                q.TenantStatus = entity.Status;
                return await _context.SaveChangesAsync() > 0;
            }
            return false;
        }
        //lay thong tin chi tiet tenant
        public async Task<Tenant?> FindById(int id)
        {
            return await _context.TblTenants
                .Where(t => t.TenantId == id)
                .Select(t => new Tenant
                {
                    Id = t.TenantId,
                    Name = t.TenantName,
                    Status = t.TenantStatus ?? 0,
                    Image = t.TenantImg,
                    TypeId = t.TenantTypeId,
                    UserId = t.TenantUserId,
                    Description = t.TenantDescription,
                    CreatedDate = (DateTime)t.TenantCreatedAt
                })
                .FirstOrDefaultAsync();
        }
        //kiem tra trung ten
        public async Task<bool> CheckTenantNameAsync(string name, int? excludeId = null)
        {
            string normalizedInput = NormalizeName(name);

            var allTenantTypeNames = await _context.TblTenants
                .Where(t => !excludeId.HasValue || t.TenantId != excludeId.Value)
                .Select(t => t.TenantName)
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
    }
}
