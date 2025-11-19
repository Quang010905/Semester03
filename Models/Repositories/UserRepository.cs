using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Semester03.Models.Entities;
using Semester03.Areas.Admin.Models;
using System.Globalization;
using System.Text;

namespace Semester03.Models.Repositories
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

        // --- New methods for registration ---

        public Task<bool> IsUsernameExistsAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return Task.FromResult(false);
            var u = username.Trim();
            return _context.TblUsers.AnyAsync(x => x.UsersUsername == u);
        }

        public Task<bool> IsEmailExistsAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return Task.FromResult(false);
            var e = email.Trim().ToLowerInvariant();
            // Translate ToLower to SQL; this is supported by EF Core
            return _context.TblUsers.AnyAsync(x => x.UsersEmail.ToLower() == e);
        }

        public Task<bool> IsPhoneExistsAsync(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return Task.FromResult(false);
            var p = phone.Trim();
            return _context.TblUsers.AnyAsync(x => x.UsersPhone == p);
        }

        /// <summary>
        /// Create new user (hashes password). UsersRoleId default = 2 (customer).
        /// Returns created entity with UsersId.
        /// </summary>
        public async Task<TblUser> CreateUserAsync(string username, string fullName, string email, string phone, string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException(nameof(username));
            if (string.IsNullOrWhiteSpace(plainPassword)) throw new ArgumentException(nameof(plainPassword));

            var user = new TblUser
            {
                UsersUsername = username.Trim(),
                UsersFullName = fullName?.Trim(),
                UsersEmail = email?.Trim().ToLowerInvariant(),
                UsersPhone = phone?.Trim(),
                UsersRoleId = 2, // role = 2 when registering
                UsersPoints = 0,
                UsersCreatedAt = DateTime.UtcNow,
                UsersUpdatedAt = DateTime.UtcNow,
                UsersStatus = 1 // <-- ensure account is active when created
            };

            user.UsersPassword = _hasher.HashPassword(user, plainPassword);

            _context.TblUsers.Add(user);
            await _context.SaveChangesAsync();

            return user;
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

        //Lay danh sach chu shop loc theo trang thai va ngay tao
        public async Task<List<User>> GetAllUserFilterByStatus()
        {
            return await _context.TblUsers
                .Select(x => new User
                {
                    Id = x.UsersId,
                    Username = x.UsersUsername,
                    Password = x.UsersPassword,
                    FullName = x.UsersFullName,
                    Email = x.UsersEmail,
                    Phone = x.UsersPhone,
                    Role = x.UsersRoleId,
                    Point = x.UsersPoints ?? 0,
                    CreatedAt = (DateTime)x.UsersCreatedAt,
                    UpdatedAt = (DateTime)x.UsersUpdatedAt,
                }).Where(x => x.Role == 3).OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
        public async Task<User?> CreateTenantByUserId(int id)
        {
            return await _context.TblUsers
                .Where(t => t.UsersId == id)
                .Select(x => new User
                {
                    Id = x.UsersId,
                    Username = x.UsersUsername,
                    Password = x.UsersPassword,
                    FullName = x.UsersFullName,
                    Email = x.UsersEmail,
                    Phone = x.UsersPhone,
                    Role = x.UsersRoleId,
                    Point = x.UsersPoints ?? 0,
                    CreatedAt = (DateTime)x.UsersCreatedAt,
                    UpdatedAt = (DateTime)x.UsersUpdatedAt,
                })
                .FirstOrDefaultAsync();
        }
    }
}
