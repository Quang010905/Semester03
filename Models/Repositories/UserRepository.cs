using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Semester03.Models.Entities;

namespace Semester03.Areas.Client.Repositories
{
    // Concrete repository (no interface used)
    public class UserRepository
    {
        private readonly AbcdmallContext _context;
        private readonly IPasswordHasher<TblUser> _hasher;

        public UserRepository(AbcdmallContext context, IPasswordHasher<TblUser> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        public Task<TblUser> GetByUsernameAsync(string username)
        {
            return _context.TblUsers
                .FirstOrDefaultAsync(u => u.UsersUsername == username);
        }

        public Task<TblUser> GetByIdAsync(int id)
        {
            return _context.TblUsers
                .FirstOrDefaultAsync(u => u.UsersId == id);
        }

        // Verify password
        public PasswordVerificationResult VerifyPassword(TblUser user, string plainPassword)
        {
            return _hasher.VerifyHashedPassword(user, user.UsersPassword ?? "", plainPassword);
        }

        // Set/update password hash
        public async Task SetPasswordHashAsync(TblUser user, string plainPassword)
        {
            user.UsersPassword = _hasher.HashPassword(user, plainPassword);
            _context.TblUsers.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
