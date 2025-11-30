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
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.ShowUpcomingAll = showUpcomingAll;

            List<EventCardVm> upcoming;

            if (showUpcomingAll)
            {
                upcoming = await _repo.GetAllUpcomingEventsAsync(fromDate, toDate);
            }
            else
            {
                upcoming = await _repo.GetUpcomingEventsAsync(6, fromDate, toDate);
            }

            var past = await _repo.GetPastEventsAsync(page, PageSize, fromDate, toDate);

            var vm = new EventHomeVm
            {
                Upcoming = upcoming,
                Past = past
            };
            ViewBag.Events = vm.Upcoming ?? new List<EventCardVm>();

            return View(vm);
        }

        public async Task<IActionResult> Details(int id, int cmtPage = 1)
        {
            int? currentUserId = GetCurrentUserId();
            const int CommentPageSize = 100;

            var ev = await _repo.GetEventByIdAsync(id, currentUserId, cmtPage, CommentPageSize);
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

            var evt = await _context.TblEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == eventId && e.EventStatus == 1);

            if (evt == null)
            {
                return Json(new { success = false, message = "The event does not exist or has been discontinued." });
            }

            var now = DateTime.Now;

            if (now < evt.EventStart)
            {
                return Json(new
                {
                    success = false,
                    message = "You can only comment after the event starts."
                });
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
