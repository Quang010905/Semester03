using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Areas.Client.Repositories;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class BookingController : Controller
    {
        private readonly IShowtimeRepository _showRepo;
        private readonly IMovieRepository _movieRepo;

        public BookingController(IShowtimeRepository showRepo, IMovieRepository movieRepo)
        {
            _showRepo = showRepo;
            _movieRepo = movieRepo;
        }

        // GET: /Client/Booking/BookTicket?movieId=1
        public IActionResult BookTicket(int movieId)
        {
            var movie = _movieRepo.GetMovieCard(movieId);
            if (movie == null) return NotFound();

            // create week array with today first (7 days)
            var today = DateTime.Now.Date; // local server time; adjust if you need user timezone
            var days = new List<DateTime>();
            for (int i = 0; i < 7; i++) days.Add(today.AddDays(i));

            var vm = new BookTicketVm
            {
                Movie = movie,
                WeekDays = days.Select(d => new DayVm
                {
                    Date = d,
                    Display = d.ToString("ddd dd MMM")
                }).ToList(),
                SelectedDate = today
            };

            return View(vm);
        }

        // ajax endpoint to load showtimes for selected day
        [HttpGet]
        public IActionResult GetShowtimes(int movieId, string date) // date = yyyy-MM-dd
        {
            if (!DateTime.TryParse(date, out var dt)) return BadRequest("Invalid date");
            var list = _showRepo.GetShowtimesForMovieOnDate(movieId, dt);
            // render partial view
            return PartialView("_ShowtimeGrid", list);
        }
    }
}
