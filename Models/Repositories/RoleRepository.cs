using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using System;

namespace Semester03.Models.Repositories
{
    public class RoleRepository
    {
    

        private readonly AbcdmallContext _context;

        public RoleRepository(AbcdmallContext context)
        {
            _context = context;
        }

        public async Task<List<TblRole>> GetAllRolesAsync()
        {
            return await _context.TblRoles.ToListAsync();
        }



        // Lấy role dựa trên người dùng
        public async Task<TblRole?> GetRoleByUserAsync(TblUser user)
        {
            if (user == null) return null;

            // Cách 1: dùng navigation property đã load sẵn
            if (user.UsersRole != null)
                return user.UsersRole;

            // Cách 2: lấy từ DB dựa vào UsersRoleId
            return await _context.TblRoles
                                 .FirstOrDefaultAsync(r => r.RolesId == user.UsersRoleId);
        }

    }

}
