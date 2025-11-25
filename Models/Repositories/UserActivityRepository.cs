using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;

namespace Semester03.Models.Repositories
{
    public class UserActivityRepository
    {
        private readonly AbcdmallContext _context;

        public UserActivityRepository(AbcdmallContext context)
        {
            _context = context;
        }

        // ===========================
        // LỊCH SỬ ĐẶT VÉ
        // ===========================
        public List<ProfileTicketVM> GetTicketHistory(int userId)
        {
            var query =
                from t in _context.TblTickets
                join ss in _context.TblShowtimeSeats on t.TicketShowtimeSeatId equals ss.ShowtimeSeatId
                join s in _context.TblSeats on ss.ShowtimeSeatSeatId equals s.SeatId
                join st in _context.TblShowtimes on ss.ShowtimeSeatShowtimeId equals st.ShowtimeId
                join m in _context.TblMovies on st.ShowtimeMovieId equals m.MovieId
                join sc in _context.TblScreens on st.ShowtimeScreenId equals sc.ScreenId
                join c in _context.TblCinemas on sc.ScreenCinemaId equals c.CinemaId
                where t.TicketBuyerUserId == userId
                orderby t.TicketCreatedAt descending
                select new ProfileTicketVM
                {
                    TicketId = t.TicketId,
                    MovieTitle = m.MovieTitle,
                    SeatLabel = s.SeatLabel,
                    ScreenName = sc.ScreenName,
                    CinemaName = c.CinemaName,
                    Showtime = st.ShowtimeStart,
                    Price = t.TicketPrice
                };

            return query.ToList();
        }

        // ===========================
        // LỊCH SỬ KHIẾU NẠI
        // ===========================
        public List<ProfileComplaintVM> GetComplaintHistory(int userId)
        {
            var query =
                from cc in _context.TblCustomerComplaints

                    // Tenant (LEFT JOIN)
                join t in _context.TblTenants
                    on cc.CustomerComplaintTenantId equals t.TenantId into tenantJoin
                from t in tenantJoin.DefaultIfEmpty()

                    // Movie (LEFT JOIN)
                join m in _context.TblMovies
                    on cc.CustomerComplaintMovieId equals m.MovieId into movieJoin
                from m in movieJoin.DefaultIfEmpty()

                    // Event (LEFT JOIN)
                join e in _context.TblEvents
                    on cc.CustomerComplaintEventId equals e.EventId into eventJoin
                from e in eventJoin.DefaultIfEmpty()

                where cc.CustomerComplaintCustomerUserId == userId
                orderby cc.CustomerComplaintCreatedAt descending

                select new ProfileComplaintVM
                {
                    ComplaintId = cc.CustomerComplaintId,
                    Rate = cc.CustomerComplaintRate,
                    Description = cc.CustomerComplaintDescription,
                    CreatedAt = cc.CustomerComplaintCreatedAt,
                    Status = cc.CustomerComplaintStatus.GetValueOrDefault(),

                    // Ưu tiên: Tenant → Movie → Event
                    TargetName =
                        t != null ? t.TenantName :
                        m != null ? m.MovieTitle :
                        e != null ? e.EventName :
                        "Không xác định"
                };

            return query.ToList();
        }
    }
}
