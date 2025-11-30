using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Partner.Models;
using Semester03.Models.Entities;

namespace Semester03.Models.Repositories
{
    public class ComplaintRepository
    {
        private readonly AbcdmallContext _context;

        public ComplaintRepository(AbcdmallContext context)
        {
            _context = context;
        }
        public async Task<List<ComPlaint>> GetAllComplaintsByEventId(int eventId)
        {
            return await _context.TblCustomerComplaints
                .Where(x => x.CustomerComplaintEventId == eventId && x.CustomerComplaintStatus == 1)
                .Select(x => new ComPlaint
                {
                    Id = x.CustomerComplaintId,
                    CustomerUserId = x.CustomerComplaintCustomerUserId,
                    TenantId = x.CustomerComplaintTenantId ?? 0,
                    Rate = x.CustomerComplaintRate,
                    Description = x.CustomerComplaintDescription,
                    Status = x.CustomerComplaintStatus ?? 0,
                    Created = x.CustomerComplaintCreatedAt ?? DateTime.MinValue,
                    MovieId = x.CustomerComplaintMovieId ?? 0,
                    EventId = x.CustomerComplaintEventId ?? 0,
                    CustomerName = x.CustomerComplaintCustomerUser.UsersFullName
                })
                .ToListAsync();
        }

        public async Task<ComPlaint?> FindById(int id)
        {
            return await _context.TblCustomerComplaints
                .Where(t => t.CustomerComplaintId == id)
                .Select(t => new ComPlaint
                {
                    Id = t.CustomerComplaintId,
                    CustomerUserId = t.CustomerComplaintCustomerUserId,
                    Status = t.CustomerComplaintStatus ?? 0,
                    TenantId = t.CustomerComplaintTenantId ?? 0,
                    Rate = t.CustomerComplaintRate,
                    Description = t.CustomerComplaintDescription,
                    Created = t.CustomerComplaintCreatedAt ?? DateTime.MinValue,
                    MovieId = t.CustomerComplaintMovieId ?? 0,
                    EventId = t.CustomerComplaintEventId ?? 0,
                    CustomerName = t.CustomerComplaintCustomerUser.UsersFullName
                })
                .FirstOrDefaultAsync();
        }


        public async Task<List<ComPlaint>> GetAllComplaintsByTenant(int tenantId)
        {
            return await _context.TblCustomerComplaints
                .Where(x => x.CustomerComplaintTenantId == tenantId && x.CustomerComplaintStatus == 1)
                .Select(x => new ComPlaint
                {
                    Id = x.CustomerComplaintId,
                    CustomerUserId = x.CustomerComplaintCustomerUserId,
                    TenantId = x.CustomerComplaintTenantId ?? 0,
                    Rate = x.CustomerComplaintRate,
                    Description = x.CustomerComplaintDescription,
                    Status = x.CustomerComplaintStatus ?? 0,
                    Created = x.CustomerComplaintCreatedAt ?? DateTime.MinValue,
                    MovieId = x.CustomerComplaintMovieId ?? 0,
                    EventId = x.CustomerComplaintEventId ?? 0,
                    CustomerName = x.CustomerComplaintCustomerUser.UsersFullName
                })
                .ToListAsync();
        }
    }
}
