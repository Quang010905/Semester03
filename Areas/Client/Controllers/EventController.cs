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

        private int? GetCurrentUserId()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated) return null;
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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

        // Index with paging + filter + view more
        public async Task<IActionResult> Index(
            int page = 1,
            bool showUpcomingAll = false,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            const int PageSize = 9;

            ViewData["Title"] = "Events";
            ViewData["MallName"] = ViewData["MallName"] ?? "ABCD Mall";

            // Lưu format yyyy-MM-dd để gán lại vào input type="date"
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.ShowUpcomingAll = showUpcomingAll;

            List<EventCardVm> upcoming;

            if (showUpcomingAll)
            {
                // View more: lấy tất cả upcoming
                upcoming = await _repo.GetAllUpcomingEventsAsync(fromDate, toDate);
            }
            else
            {
                // Mặc định: chỉ lấy top 6 upcoming
                upcoming = await _repo.GetUpcomingEventsAsync(6, fromDate, toDate);
            }

            var past = await _repo.GetPastEventsAsync(page, PageSize, fromDate, toDate);

            var vm = new EventHomeVm
            {
                Upcoming = upcoming,
                Past = past
            };

            // Dùng cho layout (carousel / sidebar)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int eventId, int rate, string text)
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return Json(new { success = false, message = "You must be logged in to comment." });
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "Unable to identify user." });
            }

            if (rate < 1 || rate > 5)
            {
                return Json(new { success = false, message = "Invalid rating (must be between 1 and 5)." });
            }

            text = (text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return Json(new { success = false, message = "Please enter your comment content." });
            }

            var evtExists = await _repo.EventExistsAsync(eventId);
            if (!evtExists)
            {
                return Json(new { success = false, message = "The event does not exist or has been discontinued." });
            }

            await _repo.AddCommentAsync(eventId, userId.Value, rate, text);

            return Json(new
            {
                success = true,
                message = "Your comment has been submitted and will be visible to everyone after approval."
            });
        }
    }
}
