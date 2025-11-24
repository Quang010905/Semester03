using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Semester03.Models.Repositories;
using Semester03.Areas.Client.Models.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize]
    public class TicketDetailController : Controller
    {
        private readonly TicketRepository _ticketRepo;

        public TicketDetailController(TicketRepository ticketRepo)
        {
            _ticketRepo = ticketRepo;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        public async Task<IActionResult> Index(int id)
        {
            int userId = GetUserId();

            var ticket = await _ticketRepo.GetByIdAsync(id);

            if (ticket == null || ticket.TicketBuyerUserId != userId)
                return NotFound();

            var st = ticket.TicketShowtimeSeat.ShowtimeSeatShowtime;
            var mv = st.ShowtimeMovie;

            bool isUsed = st.ShowtimeStart <= DateTime.Now;

            string qrUrl = Url.Action("Index", "TicketDetail",
                new { area = "Client", id = ticket.TicketId },
                protocol: HttpContext.Request.Scheme);

            string qrImg = "https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=" +
                           System.Net.WebUtility.UrlEncode(qrUrl);

            var vm = new TicketDetailVm
            {
                TicketId = ticket.TicketId,
                MovieTitle = mv.MovieTitle,
                MovieImg = mv.MovieImg ?? "/images/movie-placeholder.jpg",
                Duration = mv.MovieDurationMin,
                Director = mv.MovieDirector,
                Genre = mv.MovieGenre,
                Description = mv.MovieDescription ?? "",
                Showtime = st.ShowtimeStart,
                EndTime = st.ShowtimeStart.AddMinutes(mv.MovieDurationMin),
                Screen = st.ShowtimeScreen.ScreenName,
                Seat = ticket.TicketShowtimeSeat.ShowtimeSeatSeat.SeatLabel,
                TheaterName = "ABCD Mall Cinema",
                TheaterAddress = "123 Main Street, City",
                QRCodeUrl = qrImg,
                IsUsed = isUsed
            };

            return View(vm);
        }
    }
}
