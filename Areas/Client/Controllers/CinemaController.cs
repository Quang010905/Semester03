using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Client.Repositories;
using Semester03.Areas.Client.Models.ViewModels;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class CinemaController : Controller
    {
        private readonly ICinemaRepository _repo;

        public CinemaController(ICinemaRepository repo)
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

        // AJAX partial to get showtimes for a movie on a date
        [HttpGet]
        public async Task<IActionResult> Showtimes(int movieId, string date = null)
        {
            DateTime dt;
            if (!DateTime.TryParse(date, out dt))
                dt = DateTime.Today;

            var shows = await _repo.GetShowtimesByMovieAsync(movieId, dt);
            return PartialView("_ShowtimesPartial", shows);
        }
    }
}
