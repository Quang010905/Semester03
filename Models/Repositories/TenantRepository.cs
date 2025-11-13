using Semester03.Models.Entities;
using Semester03.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

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




    }
}
