using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Semester03.Models.Repositories
{
    public class TicketRepository
    {
        private readonly AbcdmallContext _context;

        public TicketRepository(AbcdmallContext context)
        {
            _context = context;
        }

        public async Task<List<TicketEmailItem>> GetTicketDetailsByShowtimeSeatIdsAsync(List<int> showtimeSeatIds)
        {
            var result = await (from t in _context.TblTickets
                                join s in _context.TblShowtimeSeats on t.TicketShowtimeSeatId equals s.ShowtimeSeatId
                                join st in _context.TblShowtimes on s.ShowtimeSeatShowtimeId equals st.ShowtimeId
                                join m in _context.TblMovies on st.ShowtimeMovieId equals m.MovieId
                                join sc in _context.TblScreens on st.ShowtimeScreenId equals sc.ScreenId
                                join c in _context.TblCinemas on sc.ScreenCinemaId equals c.CinemaId
                                where showtimeSeatIds.Contains(s.ShowtimeSeatId)
                                select new TicketEmailItem
                                {
                                    MovieTitle = m.MovieTitle,
                                    CinemaName = c.CinemaName,
                                    ScreenName = sc.ScreenName,
                                    ShowtimeStart = st.ShowtimeStart,
                                    SeatLabel = s.ShowtimeSeatSeat.SeatLabel,
                                    Price = st.ShowtimePrice
                                }).ToListAsync();

            return result;
        }
    }
}
