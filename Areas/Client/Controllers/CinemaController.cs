using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Client.Models.ViewModels;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class CinemaController : ClientBaseController
    {
        private readonly CinemaRepository _repo;

        public CinemaController(CinemaRepository repo, TenantTypeRepository tenantTypeRepo)
            : base(tenantTypeRepo)
        {
            _repo = repo;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Cinema";
            ViewData["MallName"] = "ABCD Mall";

            var vm = new CinemaHomeVm
            {
                Featured = await _repo.GetFeaturedMoviesAsync(3),
                NowShowing = await _repo.GetNowShowingAsync()
            };

            return View(vm); // Areas/Client/Views/Cinema/Index.cshtml
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _repo.GetMovieDetailsAsync(id);
            if (vm == null) return NotFound();
            return View(vm); // Areas/Client/Views/Cinema/Details.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int movieId, int rate, string text)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized(new { success = false, message = "Bạn cần đăng nhập để bình luận." });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                // nếu không có claim phù hợp, trả lỗi
                return Unauthorized(new { success = false, message = "Không xác định user." });
            }

            // Thêm comment (mặc định pending)
            await _repo.AddCommentAsync(movieId, userId, rate, text);

            return Json(new { success = true, message = "Cảm ơn bạn! Bình luận sẽ xuất hiện sau khi được duyệt." });
        }

        [HttpGet]
        public async Task<IActionResult> DebugNowShowing()  
        {
            var list = await _repo.GetNowShowingAsync();
            return Json(new { count = list.Count, items = list.Select(x => new { x.Id, x.Title, x.NextShowtime, x.NextShowtimeId }) });
        }

    }
}
