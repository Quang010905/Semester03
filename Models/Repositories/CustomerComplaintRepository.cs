using Semester03.Models.Entities;


using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Semester03.Areas.Admin.Models;
using System.Globalization;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace Semester03.Models.Repositories
{
    public class CustomerComplaintRepository
    {
        private readonly AbcdmallContext _context;


        public CustomerComplaintRepository(AbcdmallContext context, IPasswordHasher<TblUser> hasher)
        {
            _context = context;
        }



        //function to get list of complaints
        public async Task<List<TblCustomerComplaint>> GetAllComplaintsAsync()
        {
            return await _context.TblCustomerComplaints
                .Include(c => c.CustomerComplaintCustomerUser)   // lấy thông tin User
                .Include(c => c.CustomerComplaintTenant)         // lấy Tenant (nếu có)
                .ToListAsync();
        }



        //Function to get complaint by ID
        public async Task<TblCustomerComplaint?> GetByIdAsync(int id)
        {
            return await _context.TblCustomerComplaints
                .Include(c => c.CustomerComplaintCustomerUser)
                .Include(c => c.CustomerComplaintTenant)
                .FirstOrDefaultAsync(c => c.CustomerComplaintId == id);
        }



        //Complaint filter function by type(Tenant / Movie / Event)
        public async Task<List<TblCustomerComplaint>> GetTenantComplaintsAsync()
        {
            return await _context.TblCustomerComplaints
                .Where(c => c.CustomerComplaintTenantId != null && c.CustomerComplaintStatus == 0)
                .Include(c => c.CustomerComplaintCustomerUser)
                .Include(c => c.CustomerComplaintTenant)
                .ToListAsync();
        }


        // Complaint Movie
        public async Task<List<TblCustomerComplaint>> GetMovieComplaintsAsync()
        {
            return await _context.TblCustomerComplaints
                .Where(c => c.CustomerComplaintMovieId != null && c.CustomerComplaintStatus == 0)
                .Include(c => c.CustomerComplaintCustomerUser)
                .Include(c => c.CustomerComplaintMovie).ToListAsync();
        }



        //Complaint Event
        public async Task<List<TblCustomerComplaint>> GetEventComplaintsAsync()
        {
            return await _context.TblCustomerComplaints
                .Where(c => c.CustomerComplaintEventId != null && c.CustomerComplaintStatus == 0)
                .Include(c => c.CustomerComplaintCustomerUser)
                .Include(c => c.CustomerComplaintEvent) 

                .ToListAsync();
        }




        public async Task<List<TblCustomerComplaint>> GetAllEventComplaintsAsync()
        {
            return await _context.TblCustomerComplaints
                .Where(c => c.CustomerComplaintEventId != null) 
                .Include(c => c.CustomerComplaintCustomerUser)  
                .Include(c => c.CustomerComplaintEvent)         
                .ToListAsync();
        }






        public void ApproveComplaint(int id)
        {
            var complaint = _context.TblCustomerComplaints.FirstOrDefault(c => c.CustomerComplaintId == id);
            if (complaint != null)
            {
                complaint.CustomerComplaintStatus = 1; // 1 = Approved / Active
                _context.SaveChanges();
            }
        }

        public void CancelComplaint(int id)
        {
            var complaint = _context.TblCustomerComplaints.FirstOrDefault(c => c.CustomerComplaintId == id);
            if (complaint != null)
            {
                _context.TblCustomerComplaints.Remove(complaint);
                _context.SaveChanges();
            }
        }



        // Approve nhiều complaint cùng lúc
        public void ApproveComplaints(List<int> complaintIds)
        {
            var complaints = _context.TblCustomerComplaints
                .Where(c => complaintIds.Contains(c.CustomerComplaintId))
                .ToList();

            foreach (var c in complaints)
            {
                c.CustomerComplaintStatus = 1; // Approved
            }

            _context.SaveChanges();
        }

        // Xóa nhiều complaint cùng lúc
        public void DeleteComplaints(List<int> complaintIds)
        {
            var complaints = _context.TblCustomerComplaints
                .Where(c => complaintIds.Contains(c.CustomerComplaintId))
                .ToList();

            _context.TblCustomerComplaints.RemoveRange(complaints);
            _context.SaveChanges();
        }
    }




}
