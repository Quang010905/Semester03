using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Semester03.Models.Repositories;
using Semester03.Models.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System;
using Semester03.Areas.Client.Models.ViewModels;
using System.Linq;
using System.Collections.Generic;

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

            if (Request.Cookies.TryGetValue("GigaMall_LastUserId", out var lastUserIdStr))
            {
                if (int.TryParse(lastUserIdStr, out var lastUserId))
                {
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
                ModelState.AddModelError("", "Please enter username and password.");
                return View();
            }

            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View();
            }

            var verify = _userRepo.VerifyPassword(user, password);
            if (verify != PasswordVerificationResult.Success && verify != PasswordVerificationResult.SuccessRehashNeeded)
            {
                ModelState.AddModelError("", "Invalid username or password.");
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

            var userIdCookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = false,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            };

            Response.Cookies.Append("GigaMall_LastUserId", user.UsersId.ToString(), userIdCookieOptions);

            if (user.UsersRoleId == 1)
            {
                // Admin area (thay Dashboard/Index bằng controller/action bạn dùng)
                return RedirectToAction("Index", "Admin", new { area = "Admin" });
            }
            else if (user.UsersRoleId == 2)
            {
                return RedirectToAction("Index", "Home", new { area = "Client" });
            }else if (user.UsersRoleId == 3)
            {
                return RedirectToAction("Index", "Shop", new { area = "Partner" });
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home", new { area = "Client" });
            }
        }

        // ------------------- REGISTER (AJAX-ready) -------------------
        [HttpGet]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([FromForm] RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // Server-side validation
            if (!ModelState.IsValid)
            {
                // If AJAX, return structured errors
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = GetErrorsFromModelState(ModelState);
                    return BadRequest(new { success = false, errors });
                }
                return View(model);
            }

            if (await _userRepo.IsUsernameExistsAsync(model.Username))
            {
                ModelState.AddModelError(nameof(model.Username), "Username already exists.");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = GetErrorsFromModelState(ModelState);
                    return BadRequest(new { success = false, errors });
                }
                return View(model);
            }

            if (await _userRepo.IsEmailExistsAsync(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email is already in use.");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = GetErrorsFromModelState(ModelState);
                    return BadRequest(new { success = false, errors });
                }
                return View(model);
            }

            // NEW: check phone uniqueness
            if (await _userRepo.IsPhoneExistsAsync(model.Phone))
            {
                ModelState.AddModelError(nameof(model.Phone), "Phone number is already in use.");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = GetErrorsFromModelState(ModelState);
                    return BadRequest(new { success = false, errors });
                }
                return View(model);
            }

            try
            {
                var created = await _userRepo.CreateUserAsync(model.Username, model.FullName, model.Email, model.Phone, model.Password);

                // Auto sign-in after register (same as login)
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, created.UsersId.ToString()),
                    new Claim(ClaimTypes.Name, created.UsersFullName ?? created.UsersUsername),
                    new Claim(ClaimTypes.Role, created.UsersRoleId.ToString())
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                var userIdCookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = false,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Path = "/"
                };
                Response.Cookies.Append("GigaMall_LastUserId", created.UsersId.ToString(), userIdCookieOptions);

                // AJAX: return JSON with redirect URL
                var successRedirectUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                                            ? returnUrl
                                            : Url.Action("Login", "Account", new { area = "Client" });

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Ok(new { success = true, redirect = successRedirectUrl });
                }

                return Redirect(successRedirectUrl);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating user during registration for {Username}", model.Username);
                ModelState.AddModelError("", "An error occurred while creating the account. Please try again later.");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = GetErrorsFromModelState(ModelState);
                    return StatusCode(500, new { success = false, errors });
                }

                return View(model);
            }
        }

        private static IDictionary<string, string[]> GetErrorsFromModelState(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ms)
        {
            return ms.Where(kvp => kvp.Value.Errors.Count > 0)
                     .ToDictionary(
                         kvp => kvp.Key.Replace("model.", "").Replace("Model.", "").Replace("model", ""),
                         kvp => kvp.Value.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message ?? "Unknown error" : e.ErrorMessage).ToArray()
                     );
        }

        // ------------------- LOGOUT unchanged -------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            _logger?.LogInformation("Logout requested for user {User}, Authenticated={Auth}", User?.Identity?.Name, User?.Identity?.IsAuthenticated);

            foreach (var key in Request.Cookies.Keys)
            {
                _logger?.LogInformation("Request cookie: {Key} = {Val}", key, Request.Cookies[key]);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync();

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

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok();
            }

            return RedirectToAction("Index", "Home", new { area = "Client" });
        }
    }
}
