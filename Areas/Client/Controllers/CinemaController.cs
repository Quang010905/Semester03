using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Client.Models.ViewModels;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Repositories;
using Semester03.Models.Entities;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class CinemaController : ClientBaseController
    {
        private readonly CinemaRepository _repo;
        private readonly AbcdmallContext _db;

        public CinemaController(
            CinemaRepository repo,
            TenantTypeRepository tenantTypeRepo,
            AbcdmallContext db)
            : base(tenantTypeRepo)
        {
            _repo = repo;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new CinemaHomeVm
            {
                // BỎ giới hạn 3, dùng default của repo (hiển thị tất cả phim trong khoảng công chiếu)
                Featured = await _repo.GetFeaturedMoviesAsync(),
                NowShowing = await _repo.GetNowShowingAsync()
            };

            // LẤY USER ID ĐÚNG CLAIM
            if (User?.Identity?.IsAuthenticated == true)
            {
                // AccountController đang set ClaimTypes.NameIdentifier = UsersId
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(userIdStr, out var userId))
                {
                    var (points, coupons) = await _repo.GetCouponsForUserAsync(userId);
                    vm.UserPoints = points;
                    vm.AvailableCoupons = coupons;
                }
            }

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            int? currentUserId = null;

            if (User?.Identity?.IsAuthenticated == true)
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? User.FindFirst("UserId")?.Value;

                if (int.TryParse(userIdStr, out var uid))
                {
                    currentUserId = uid;
                }
            }

            var vm = await _repo.GetMovieDetailsAsync(id, currentUserId);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int movieId, int rate, string text)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized(new { success = false, message = "You need to log in to comment." });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("UserId")?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "User not identified." });
            }

            await _repo.AddCommentAsync(movieId, userId, rate, text);

            return Json(new { success = true, message = "Thank you! Your comment will appear after it has been approved." });
        }

        [HttpGet]
        public async Task<IActionResult> DebugNowShowing()
        {
            var list = await _repo.GetNowShowingAsync();
            return Json(new
            {
                count = list.Count,
                items = list.Select(x => new { x.Id, x.Title, x.NextShowtime, x.NextShowtimeId })
            });
        }
    }
}
