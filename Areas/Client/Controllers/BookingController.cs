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
        private readonly ISeatRepository _seatRepo;

        public BookingController(IShowtimeRepository showRepo, IMovieRepository movieRepo, ISeatRepository seatRepo)
        {
            _showRepo = showRepo;
            _movieRepo = movieRepo;
            _seatRepo = seatRepo;
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

        public IActionResult SelectSeat(int showtimeId)
        {
            var vm = _seatRepo.GetSeatLayoutForShowtime(showtimeId);
            if (vm == null || vm.Seats == null) return NotFound();
            return View(vm);
        }

        // POST: /Client/Booking/ReserveSeats
        [HttpPost]
        public IActionResult ReserveSeats([FromBody] ReserveRequestVm req)
        {
            if (req == null || req.ShowtimeSeatIds == null || !req.ShowtimeSeatIds.Any())
                return BadRequest(new { success = false, message = "No seats selected" });

            // for demo, use current user id if signed in - otherwise null
            int? userId = null;
            if (User?.Identity?.IsAuthenticated == true)
            {
                // replace with your user id retrieval
                // userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            }

            var (succeeded, failed) = _seatRepo.ReserveSeats(req.ShowtimeId, req.ShowtimeSeatIds, userId);

            var refreshed = _seatRepo.RefreshSeatLayout(req.ShowtimeId);

            return Ok(new
            {
                success = succeeded.Any(),
                succeeded,
                failed,
                layout = refreshed
            });
        }
    }
}
