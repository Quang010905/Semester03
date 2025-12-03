using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;

namespace Semester03.Models.Repositories
{
    public class CinemaRepository
    {
        private readonly AbcdmallContext _db;
        public CinemaRepository(AbcdmallContext db) => _db = db;

        // ==================== FEATURED ====================
        // Lấy tất cả phim đang công bố chiếu (status = 1, trong Start/End),
        // và nếu có thì kèm suất chiếu sắp tới nhất.
        public async Task<List<MovieCardVm>> GetFeaturedMoviesAsync(int top = int.MaxValue)
        {
            var now = DateTime.Now;
            var today = DateTime.Today;

            // Phim đang công bố chiếu
            var movies = _db.TblMovies.AsNoTracking()
                .Where(m =>
                    (m.MovieStatus ?? 0) == 1 &&
                    (m.MovieStartDate == null || m.MovieStartDate <= today) &&
                    (m.MovieEndDate == null || m.MovieEndDate >= today)
                );

            // Suất chiếu tương lai (từ bây giờ trở đi) để lấy suất gần nhất cho mỗi phim
            var futureShowtimes = _db.TblShowtimes.AsNoTracking()
                .Where(s => s.ShowtimeStart >= now)
                .GroupBy(s => s.ShowtimeMovieId)
                .Select(g => new
                {
                    MovieId = g.Key,
                    NextShowtime = (DateTime?)g.Min(x => x.ShowtimeStart),
                    NextShowtimeId = (int?)g
                        .OrderBy(x => x.ShowtimeStart)
                        .ThenBy(x => x.ShowtimeId)
                        .Select(x => x.ShowtimeId)
                        .FirstOrDefault(),
                    NextPrice = (decimal?)g
                        .OrderBy(x => x.ShowtimeStart)
                        .ThenBy(x => x.ShowtimeId)
                        .Select(x => x.ShowtimePrice)
                        .FirstOrDefault()
                });

            var q = from m in movies
                    join fs in futureShowtimes
                        on m.MovieId equals fs.MovieId into gj
                    from fs in gj.DefaultIfEmpty()
                    select new
                    {
                        m.MovieId,
                        m.MovieTitle,
                        m.MovieDescription,
                        m.MovieDurationMin,
                        m.MovieImg,
                        NextShowtime = fs != null ? fs.NextShowtime : (DateTime?)null,
                        NextShowtimeId = fs != null ? fs.NextShowtimeId : (int?)null,
                        NextPrice = fs != null ? fs.NextPrice : (decimal?)null
                    };

            q = q
                .OrderBy(x => x.NextShowtime == null ? 1 : 0)
                .ThenBy(x => x.NextShowtime ?? DateTime.MaxValue);

            var data = (top == int.MaxValue)
                ? await q.ToListAsync()
                : await q.Take(top).ToListAsync();

            return data.Select(x => new MovieCardVm
            {
                Id = x.MovieId,
                Title = x.MovieTitle ?? "",
                Description = x.MovieDescription ?? "",
                DurationMin = x.MovieDurationMin,
                NextShowtime = x.NextShowtime,
                NextPrice = x.NextPrice,
                NextShowtimeId = x.NextShowtimeId,
                PosterUrl = string.IsNullOrWhiteSpace(x.MovieImg)
                    ? "/images/movie-placeholder.png"
                    : x.MovieImg,
                ScreenName = null // Featured không cần Screen cụ thể
            }).ToList();
        }

        // ==================== NOW SHOWING ====================
        // Chỉ lấy phim status = 1, còn trong khoảng start/end,
        // có suất chiếu từ bây giờ đến 7 ngày tới,
        // và lấy suất gần nhất + ScreenName.
        public async Task<List<MovieCardVm>> GetNowShowingAsync()
        {
            var now = DateTime.Now;
            var today = now.Date;
            var endDate = today.AddDays(7); // hôm nay + 6 ngày (7 ngày tổng)

            // Bước 1: tìm NextStart (suất gần nhất) cho từng movie trong khoảng [now, endDate)
            var nextStarts = from s in _db.TblShowtimes.AsNoTracking()
                             join m in _db.TblMovies.AsNoTracking()
                                 on s.ShowtimeMovieId equals m.MovieId
                             where
                                 s.ShowtimeStart >= now &&
                                 s.ShowtimeStart < endDate &&
                                 (m.MovieStatus ?? 0) == 1 &&
                                 (m.MovieStartDate == null || m.MovieStartDate <= today) &&
                                 (m.MovieEndDate == null || m.MovieEndDate >= today)
                             group s by s.ShowtimeMovieId into g
                             select new
                             {
                                 MovieId = g.Key,
                                 NextStart = g.Min(x => x.ShowtimeStart)
                             };

            // Bước 2: join lại để lấy thông tin đầy đủ + ScreenName
            var q = from ns in nextStarts
                    join s in _db.TblShowtimes.AsNoTracking()
                        on new { MovieId = ns.MovieId, NextStart = ns.NextStart }
                        equals new { MovieId = s.ShowtimeMovieId, NextStart = s.ShowtimeStart }
                    join m in _db.TblMovies.AsNoTracking()
                        on ns.MovieId equals m.MovieId
                    join scr in _db.TblScreens.AsNoTracking()
                        on s.ShowtimeScreenId equals scr.ScreenId
                    where
                        (m.MovieStatus ?? 0) == 1 &&
                        (m.MovieStartDate == null || m.MovieStartDate <= today) &&
                        (m.MovieEndDate == null || m.MovieEndDate >= today)
                    orderby s.ShowtimeStart
                    select new
                    {
                        m.MovieId,
                        m.MovieTitle,
                        m.MovieDescription,
                        m.MovieDurationMin,
                        m.MovieImg,
                        ShowtimeStart = s.ShowtimeStart,
                        ShowtimePrice = s.ShowtimePrice,
                        ShowtimeId = s.ShowtimeId,
                        ScreenName = scr.ScreenName
                    };

            var list = await q.ToListAsync();

            return list.Select(x => new MovieCardVm
            {
                Id = x.MovieId,
                Title = x.MovieTitle ?? "",
                Description = x.MovieDescription ?? "",
                DurationMin = x.MovieDurationMin,
                NextShowtime = x.ShowtimeStart,
                NextPrice = x.ShowtimePrice,
                NextShowtimeId = x.ShowtimeId,
                PosterUrl = string.IsNullOrWhiteSpace(x.MovieImg)
                    ? "/images/movie-placeholder.png"
                    : x.MovieImg,
                ScreenName = x.ScreenName
            }).ToList();
        }

        // ==================== CHI TIẾT PHIM + COMMENT ====================
        public async Task<MovieDetailsVm> GetMovieDetailsAsync(
            int movieId,
            int? currentUserId,
            int commentPage,
            int pageSize)
        {
            if (commentPage < 1) commentPage = 1;
            if (pageSize < 1) pageSize = 5;

            var movie = await _db.TblMovies
                .AsNoTracking()
                .Where(m => m.MovieId == movieId)
                .Select(m => new MovieDetailsVm
                {
                    Id = m.MovieId,
                    Title = m.MovieTitle,
                    Genre = m.MovieGenre,
                    Director = m.MovieDirector,
                    PosterUrl = m.MovieImg,
                    StartDate = m.MovieStartDate,
                    EndDate = m.MovieEndDate,
                    Rate = m.MovieRate,
                    DurationMin = m.MovieDurationMin,
                    Description = m.MovieDescription
                }).FirstOrDefaultAsync();

            if (movie == null) return null;

            // ====== BASE QUERY COMMENT ======
            var commentsQuery = _db.TblCustomerComplaints
                .AsNoTracking()
                .Where(c => c.CustomerComplaintMovieId == movieId);

            if (currentUserId.HasValue)
            {
                var uid = currentUserId.Value;
                commentsQuery = commentsQuery.Where(c =>
                    c.CustomerComplaintStatus == 1 ||
                    c.CustomerComplaintCustomerUserId == uid);
            }
            else
            {
                commentsQuery = commentsQuery.Where(c => c.CustomerComplaintStatus == 1);
            }

            var totalComments = await commentsQuery.CountAsync();

            var comments = await commentsQuery
                .OrderByDescending(c => c.CustomerComplaintCreatedAt)
                .Skip((commentPage - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CommentVm
                {
                    Id = c.CustomerComplaintId,
                    UserId = c.CustomerComplaintCustomerUserId,
                    UserName = c.CustomerComplaintCustomerUser != null
                        ? (string.IsNullOrWhiteSpace(c.CustomerComplaintCustomerUser.UsersFullName)
                            ? c.CustomerComplaintCustomerUser.UsersUsername
                            : c.CustomerComplaintCustomerUser.UsersFullName)
                        : "Khách",
                    Rate = c.CustomerComplaintRate,
                    Text = c.CustomerComplaintDescription,
                    CreatedAt = c.CustomerComplaintCreatedAt
                })
                .ToListAsync();

            movie.Comments = comments;
            movie.CommentCount = totalComments;
            movie.CommentPageIndex = commentPage;
            movie.CommentPageSize = pageSize;
            movie.CommentTotalPages = pageSize == 0
                ? 0
                : (int)Math.Ceiling(totalComments / (double)pageSize);

            return movie;
        }

        public async Task AddCommentAsync(int movieId, int userId, int rate, string text)
        {
            var ent = new TblCustomerComplaint
            {
                CustomerComplaintCustomerUserId = userId,
                CustomerComplaintMovieId = movieId,
                CustomerComplaintRate = rate,
                CustomerComplaintDescription = text,
                CustomerComplaintStatus = 0,      // 0 = chờ duyệt
                CustomerComplaintCreatedAt = DateTime.Now
            };

            _db.TblCustomerComplaints.Add(ent);
            await _db.SaveChangesAsync();
        }

        public async Task<(int userPoints, List<CouponVm> coupons)> GetCouponsForUserAsync(int userId)
        {
            var now = DateTime.Now;

            var user = await _db.TblUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UsersId == userId);

            if (user == null)
                return (0, new List<CouponVm>());

            var userPoints = user.UsersPoints ?? 0;

            var coupons = await _db.TblCoupons
                .AsNoTracking()
                .Where(c =>
                    c.CouponIsActive == true &&
                    c.CouponValidFrom <= now &&
                    c.CouponValidTo >= now &&
                    (c.CouponMinimumPointsRequired == null ||
                     c.CouponMinimumPointsRequired <= userPoints))
                .Select(c => new CouponVm
                {
                    Id = c.CouponId,
                    Name = c.CouponName,
                    Description = c.CouponDescription,
                    DiscountPercent = c.CouponDiscountPercent,
                    ValidFrom = c.CouponValidFrom,
                    ValidTo = c.CouponValidTo,
                    MinimumPointsRequired = c.CouponMinimumPointsRequired
                })
                .OrderBy(c => c.ValidTo)
                .ToListAsync();

            return (userPoints, coupons);
        }

        private const int FIXED_CINEMA_ID = 1;

        public async Task<TblCinema> GetSettingsAsync()
        {
            var cinema = await _db.TblCinemas.FindAsync(FIXED_CINEMA_ID);

            if (cinema == null)
            {
                cinema = new TblCinema
                {
                    CinemaId = FIXED_CINEMA_ID,
                    CinemaName = "Galaxy ABCD Mall"
                };
                _db.TblCinemas.Add(cinema);
                await _db.SaveChangesAsync();
            }
            return cinema;
        }

        public async Task UpdateSettingsAsync(TblCinema cinemaSettings)
        {
            cinemaSettings.CinemaId = FIXED_CINEMA_ID;
            _db.Entry(cinemaSettings).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public async Task<List<MovieCardVm>> GetMoviesByDateAsync(DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            var today = DateTime.Today;

            // Phim đang công bố chiếu (status 1 + trong khoảng start/end)
            var baseMovies = _db.TblMovies.AsNoTracking()
                .Where(m =>
                    (m.MovieStatus ?? 0) == 1 &&
                    (m.MovieStartDate == null || m.MovieStartDate <= today) &&
                    (m.MovieEndDate == null || m.MovieEndDate >= today)
                );

            var q = from s in _db.TblShowtimes.AsNoTracking()
                    join m in baseMovies on s.ShowtimeMovieId equals m.MovieId
                    join scr in _db.TblScreens.AsNoTracking() on s.ShowtimeScreenId equals scr.ScreenId
                    where s.ShowtimeStart >= dayStart && s.ShowtimeStart < dayEnd
                    group new { m, s, scr } by new
                    {
                        m.MovieId,
                        m.MovieTitle,
                        m.MovieDescription,
                        m.MovieDurationMin,
                        m.MovieImg
                    }
                into g
                    select new
                    {
                        g.Key.MovieId,
                        g.Key.MovieTitle,
                        g.Key.MovieDescription,
                        g.Key.MovieDurationMin,
                        g.Key.MovieImg,
                        NextShowtime = (DateTime?)g.Min(x => x.s.ShowtimeStart),
                        NextShowtimeId = (int?)g
                            .OrderBy(x => x.s.ShowtimeStart)
                            .ThenBy(x => x.s.ShowtimeId)
                            .Select(x => x.s.ShowtimeId)
                            .FirstOrDefault(),
                        NextPrice = (decimal?)g
                            .OrderBy(x => x.s.ShowtimeStart)
                            .ThenBy(x => x.s.ShowtimeId)
                            .Select(x => x.s.ShowtimePrice)
                            .FirstOrDefault(),
                        ScreenName = g
                            .OrderBy(x => x.s.ShowtimeStart)
                            .ThenBy(x => x.s.ShowtimeId)
                            .Select(x => x.scr.ScreenName)
                            .FirstOrDefault()
                    };

            var list = await q
                .OrderBy(x => x.NextShowtime)
                .ThenBy(x => x.MovieTitle)
                .ToListAsync();

            return list.Select(x => new MovieCardVm
            {
                Id = x.MovieId,
                Title = x.MovieTitle ?? "",
                Description = x.MovieDescription ?? "",
                DurationMin = x.MovieDurationMin,
                NextShowtime = x.NextShowtime,
                NextPrice = x.NextPrice,
                NextShowtimeId = x.NextShowtimeId,
                PosterUrl = string.IsNullOrWhiteSpace(x.MovieImg)
                    ? "/images/movie-placeholder.png"
                    : x.MovieImg,
                ScreenName = x.ScreenName
            }).ToList();
        }

    }
}
