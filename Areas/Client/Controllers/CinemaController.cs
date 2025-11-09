using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Client.Repositories;
using Semester03.Areas.Client.Models.ViewModels;
using System;
using System.Threading.Tasks;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class CinemaController : Controller
    {
        private readonly CinemaRepository _repo;
        public CinemaController(CinemaRepository repo) => _repo = repo;

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
        public async Task<IActionResult> Showtimes(int movieId, string date = null)
        {
            DateTime dt;
            if (!DateTime.TryParse(date, out dt))
                dt = DateTime.Today;

            var shows = await Task.Run(() => // repository is synchronous for showtimes; ShowtimeRepository handles date logic
            {
                var showRepo = HttpContext.RequestServices.GetService(typeof(ShowtimeRepository)) as ShowtimeRepository;
                return showRepo?.GetShowtimesForMovieOnDate(movieId, dt) ?? new System.Collections.Generic.List<ShowtimeVm>();
            });

            return PartialView("_ShowtimesPartial", shows);
        }
    }
}
