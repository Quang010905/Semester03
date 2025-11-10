using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Semester03.Areas.Client.Repositories;
using Semester03.Models.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    [Route("Client/[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly UserRepository _userRepo;
        private readonly ILogger<AccountController> _logger;
        private readonly IPasswordHasher<TblUser> _hasher;

        public AccountController(UserRepository userRepo, IPasswordHasher<TblUser> hasher, ILogger<AccountController> logger)
        {
            _userRepo = userRepo;
            _hasher = hasher;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            // simple page; returnUrl để redirect sau login nếu cần
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập tên đăng nhập và mật khẩu.");
                return View();
            }

            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View();
            }

            var verify = _userRepo.VerifyPassword(user, password);
            if (verify != PasswordVerificationResult.Success && verify != PasswordVerificationResult.SuccessRehashNeeded)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View();
            }

            // Nếu cần rehash (PasswordVerificationResult.SuccessRehashNeeded) -> update hash
            if (verify == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.UsersPassword = _hasher.HashPassword(user, password);
                // lưu lại hash mới
                try
                {
                    // We do not have a repo method to update generic fields; we can set via repository method
                    await _userRepo.SetPasswordHashAsync(user, password);
                }
                catch (System.Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to rehash password for user {UserId}", user.UsersId);
                }
            }

            // tạo claims: lưu UserID trong claim NameIdentifier
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UsersId.ToString()),
                new Claim(ClaimTypes.Name, user.UsersFullName ?? user.UsersUsername),
                // lưu role id numeric trong Claim Role
                new Claim(ClaimTypes.Role, user.UsersRoleId.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = System.DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            // redirect theo role
            if (user.UsersRoleId == 1)
            {
                // Admin area (thay Dashboard/Index bằng controller/action bạn dùng)
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            else if (user.UsersRoleId == 2)
            {
                // Client -> cinema index
                return RedirectToAction("Index", "Cinema", new { area = "Client" });
            }
            else
            {
                // default fallback
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home", new { area = "Client" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home", new { area = "Client" });
        }
    }
}
