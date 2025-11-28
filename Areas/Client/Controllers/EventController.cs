using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class EventController : ClientBaseController
    {
        private readonly EventRepository _repo;
        private readonly AbcdmallContext _context;
        private readonly ILogger<EventController> _logger;

        public EventController(
            TenantTypeRepository tenantTypeRepo,
            EventRepository repo,
            AbcdmallContext context,
            ILogger<EventController> logger = null
        ) : base(tenantTypeRepo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
        }

        // Helper: lấy user id hiện tại an toàn từ claims
        private int? GetCurrentUserId()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return null;

            // ưu tiên NameIdentifier (đây là claim bạn set khi sign-in)
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // fallback: một số hệ thống có thể dùng "UserId" hoặc DefaultNameClaimType
            if (string.IsNullOrWhiteSpace(id))
            {
                id = User.FindFirst("UserId")?.Value;
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                id = User.FindFirst(ClaimsIdentity.DefaultNameClaimType)?.Value;
            }

            if (int.TryParse(id, out var uid)) return uid;
            return null;
        }

        // Trang danh sách sự kiện
        public async Task<IActionResult> Index(int page = 1)
        {
            const int PageSize = 9; // 3 cột * 3 hàng

            ViewData["Title"] = "Events";
            ViewData["MallName"] = ViewData["MallName"] ?? "ABCD Mall";

            var vm = new EventHomeVm
            {
                Upcoming = await _repo.GetUpcomingEventsAsync(),
                Past = await _repo.GetPastEventsAsync(page, PageSize)
            };

            ViewBag.Events = vm.Upcoming ?? new List<EventCardVm>();

            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            int? currentUserId = GetCurrentUserId();

            var ev = await _repo.GetEventByIdAsync(id, currentUserId);
            if (ev == null)
                return NotFound();

            ViewData["Title"] = ev.Title;
            ViewBag.Events = await _repo.GetUpcomingEventsAsync() ?? new List<EventCardVm>();

            return View(ev);
        }

        // ⭐ COMMENT EVENT – AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int eventId, int rate, string text)
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return Json(new { success = false, message = "You need to log in to comment." });
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                // optional: log claims for debugging (uncomment if needed)
                /*
                _logger?.LogWarning("AddComment: Unable to parse user id from claims. Claims: {claims}",
                    string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
                */
                return Json(new { success = false, message = "User cannot be identified.." });
            }

            if (rate < 1 || rate > 5)
            {
                return Json(new { success = false, message = "Invalid rating (1-5)." });
            }

            text = (text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return Json(new { success = false, message = "Please enter a comment." });
            }

            // Sử dụng repository để kiểm tra event tồn tại
            var evtExists = await _repo.EventExistsAsync(eventId);

            if (!evtExists)
            {
                return Json(new { success = false, message = "The event does not exist or has ended." });
            }

            // Gọi repository để thêm comment
            await _repo.AddCommentAsync(eventId, userId.Value, rate, text);

            return Json(new
            {
                success = true,
                message = "Your comment has been submitted and will be visible to everyone once approved."
            });
        }
    }
}
