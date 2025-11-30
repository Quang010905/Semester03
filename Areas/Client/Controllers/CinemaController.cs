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
        public async Task<IActionResult> Details(int id, int cmtPage = 1)
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

            const int CommentPageSize = 100;

            var vm = await _repo.GetMovieDetailsAsync(id, currentUserId, cmtPage, CommentPageSize);
            if (vm == null) return NotFound();
            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int movieId, int rate, string text)
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
                return Unauthorized(new { success = false, message = "You need to log in to comment." });

      
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("UserId")?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "User not identified." });
            }

            if (rate < 1 || rate > 5)
            {
                return Json(new { success = false, message = "Invalid rating (must be between 1 and 5)." });
            }

            text = (text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return Json(new { success = false, message = "Please write a comment." });
            }
            var movie = await _db.TblMovies
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MovieId == movieId);

            if (movie == null)
            {
                return Json(new { success = false, message = "Movie does not exist." });
            }
            var now = DateTime.Now;

            var hasPastShowtime = await _db.TblShowtimes
                .AsNoTracking()
                .AnyAsync(s => s.ShowtimeMovieId == movieId && s.ShowtimeStart <= now);

            if (!hasPastShowtime)
            {
                return Json(new
                {
                    success = false,
                    message = "You can only comment when the movie already has at least one showtime that has been shown."
                });
            }
            await _repo.AddCommentAsync(movieId, userId, rate, text);

            return Json(new
            {
                success = true,
                message = "Thank you! Your comment will appear after it has been approved."
            });
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
