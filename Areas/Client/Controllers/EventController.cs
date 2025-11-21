using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class EventController : Controller
    {
        private readonly EventRepository _repo;
        private readonly AbcdmallContext _context;

        public EventController(EventRepository repo, AbcdmallContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Events";
            ViewData["MallName"] = ViewData["MallName"] ?? "ABCD Mall";

            var vm = new EventHomeVm
            {
                Featured = await _repo.GetFeaturedEventsAsync(6),
                Upcoming = await _repo.GetUpcomingEventsAsync()
            };

            ViewBag.Events = vm.Upcoming ?? new List<EventCardVm>();

            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            int? currentUserId = null;
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out var uid))
            {
                currentUserId = uid;
            }

            var ev = await _repo.GetEventByIdAsync(id, currentUserId);
            if (ev == null)
                return NotFound();

            ViewData["Title"] = ev.Title;
            ViewBag.Events = await _repo.GetUpcomingEventsAsync() ?? new List<EventCardVm>();

            return View(ev);
        }

        // ⭐ COMMENT EVENT – AJAX giống Store/Product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int eventId, int rate, string text)
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return Json(new { success = false, message = "Bạn phải đăng nhập để bình luận." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Json(new { success = false, message = "Không xác định được người dùng." });
            }

            if (rate < 1 || rate > 5)
            {
                return Json(new { success = false, message = "Điểm đánh giá không hợp lệ (1-5)." });
            }

            text = (text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return Json(new { success = false, message = "Vui lòng nhập nội dung bình luận." });
            }

            var evtExists = await _context.TblEvents
                .AsNoTracking()
                .AnyAsync(e => e.EventId == eventId && e.EventStatus == 1);

            if (!evtExists)
            {
                return Json(new { success = false, message = "Sự kiện không tồn tại hoặc đã ngừng." });
            }

            var entity = new TblCustomerComplaint
            {
                CustomerComplaintCustomerUserId = userId,
                CustomerComplaintTenantId = null,
                CustomerComplaintMovieId = null,
                CustomerComplaintEventId = eventId,
                CustomerComplaintRate = rate,
                CustomerComplaintDescription = text,
                CustomerComplaintStatus = 0,
                CustomerComplaintCreatedAt = DateTime.UtcNow
            };

            _context.TblCustomerComplaints.Add(entity);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Bình luận đã được gửi, sẽ hiển thị cho mọi người sau khi được duyệt."
            });
        }
    }
}
