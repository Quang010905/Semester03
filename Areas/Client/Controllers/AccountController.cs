using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Semester03.Areas.Client.Repositories;
using Semester03.Models.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System;

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
            ViewData["ReturnUrl"] = returnUrl;

            // Optional: prefill username if you stored last user id cookie (server-side)
            if (Request.Cookies.TryGetValue("GigaMall_LastUserId", out var lastUserIdStr))
            {
                if (int.TryParse(lastUserIdStr, out var lastUserId))
                {
                    // Avoid sync-over-async in production; this is optional convenience.
                    var u = _userRepo.GetByIdAsync(lastUserId).GetAwaiter().GetResult();
                    if (u != null)
                    {
                        ViewData["PrefillUsername"] = u.UsersUsername;
                    }
                }
            }

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

            if (verify == PasswordVerificationResult.SuccessRehashNeeded)
            {
                try
                {
                    await _userRepo.SetPasswordHashAsync(user, password);
                }
                catch (System.Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to rehash password for user {UserId}", user.UsersId);
                }
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UsersId.ToString()),
                new Claim(ClaimTypes.Name, user.UsersFullName ?? user.UsersUsername),
                new Claim(ClaimTypes.Role, user.UsersRoleId.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            // Store a lightweight cookie with last user id for non-greeting purposes.
            // IMPORTANT: Do NOT use this cookie client-side to show "Xin chào" — UI greeting must rely on authentication.
            var userIdCookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = false, // set to false if JS needs to read it for other features; true if only server reads it
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            };

            Response.Cookies.Append("GigaMall_LastUserId", user.UsersId.ToString(), userIdCookieOptions);

            // redirect theo role
            if (user.UsersRoleId == 1)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            else if (user.UsersRoleId == 2)
            {
                return RedirectToAction("Index", "Cinema", new { area = "Client" });
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home", new { area = "Client" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            _logger?.LogInformation("Logout requested for user {User}, Authenticated={Auth}", User?.Identity?.Name, User?.Identity?.IsAuthenticated);

            // Log request cookies for debugging
            foreach (var key in Request.Cookies.Keys)
            {
                _logger?.LogInformation("Request cookie: {Key} = {Val}", key, Request.Cookies[key]);
            }

            // Sign out the authentication cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Fallback sign out
            await HttpContext.SignOutAsync();

            // Delete auth cookie by name (must match cookie name in Program.cs)
            var authCookieName = ".AspNetCore.Cookies";
            Response.Cookies.Delete(authCookieName);
            Response.Cookies.Append(authCookieName, "", new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(-1),
                HttpOnly = true,
                Secure = Request.IsHttps,
                Path = "/",
                SameSite = SameSiteMode.Lax
            });

            // Optionally delete the helper cookie "GigaMall_LastUserId" if you want logout to remove it.
            // If you prefer to keep it (for prefill username next time), comment these two lines out.
            Response.Cookies.Delete("GigaMall_LastUserId");
            Response.Cookies.Append("GigaMall_LastUserId", "", new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(-1),
                HttpOnly = false,
                Secure = Request.IsHttps,
                Path = "/",
                SameSite = SameSiteMode.Lax
            });

            _logger?.LogInformation("Logout finished.");

            // If AJAX request, return 200 OK so client JS can update UI without redirect
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok();
            }

            return RedirectToAction("Index", "Home", new { area = "Client" });
        }
    }
}
