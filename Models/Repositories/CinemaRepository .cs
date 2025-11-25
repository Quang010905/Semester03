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

        public async Task<List<MovieCardVm>> GetFeaturedMoviesAsync(int top = 3)
        {
            var now = DateTime.Now;

            var nextStarts = from s in _db.TblShowtimes.AsNoTracking()
                             join m in _db.TblMovies.AsNoTracking()
                                 on s.ShowtimeMovieId equals m.MovieId
                             where s.ShowtimeStart >= now
                                && (m.MovieStartDate == null || m.M‌​ovieStartDate <= now)
                                && (m.MovieEndDate == null || m.MovieEndDate >= now)
                             group s by s.ShowtimeMovieId into g
                             select new
                             {
                                 MovieId = g.Key,
                                 NextStart = g.Min(s => s.ShowtimeStart)
                             };

            var q = from ns in nextStarts
                    join s in _db.TblShowtimes.AsNoTracking()
                        on new { MovieId = ns.MovieId, NextStart = ns.NextStart }
                        equals new { MovieId = s.ShowtimeMovieId, NextStart = s.ShowtimeStart }
                    join m in _db.TblMovies.AsNoTracking() on ns.MovieId equals m.MovieId
                    join scr in _db.TblScreens.AsNoTracking() on s.ShowtimeScreenId equals scr.ScreenId
                    join c in _db.TblCinemas.AsNoTracking() on scr.ScreenCinemaId equals c.CinemaId
                    where (m.MovieStartDate == null || m.MovieStartDate <= now)
                          && (m.MovieEndDate == null || m.MovieEndDate >= now)
                    orderby s.ShowtimeStart
                    select new
                    {
                        MovieId = m.MovieId,
                        MovieTitle = m.MovieTitle,
                        MovieDescription = m.MovieDescription,
                        MovieDurationMin = m.MovieDurationMin,
                        ShowtimeStart = s.ShowtimeStart,
                        ShowtimePrice = s.ShowtimePrice,
                        ShowtimeId = s.ShowtimeId,
                        PosterUrl = m.MovieImg,
                        CinemaName = c.CinemaName,
                        ScreenName = scr.ScreenName
                    };

            var list = await q.Take(top).ToListAsync();

            return list.Select(x => new MovieCardVm
            {
                Id = x.MovieId,
                Title = x.MovieTitle ?? "",
                Description = x.MovieDescription ?? "",
                DurationMin = x.MovieDurationMin,
                NextShowtime = x.ShowtimeStart,
                NextPrice = x.ShowtimePrice,
                NextShowtimeId = x.ShowtimeId,
                PosterUrl = string.IsNullOrWhiteSpace(x.PosterUrl) ? "/images/movie-placeholder.png" : x.PosterUrl
            }).ToList();
        }

        public async Task<List<MovieCardVm>> GetNowShowingAsync()
        {
            var now = DateTime.Now;

            var nextStarts = from s in _db.TblShowtimes.AsNoTracking()
                             join m in _db.TblMovies.AsNoTracking()
                                 on s.ShowtimeMovieId equals m.MovieId
                             where s.ShowtimeStart >= now
                                && (m.MovieStartDate == null || m.MovieStartDate <= now)
                                && (m.MovieEndDate == null || m.MovieEndDate >= now)
                             group s by s.ShowtimeMovieId into g
                             select new
                             {
                                 MovieId = g.Key,
                                 NextStart = g.Min(s => s.ShowtimeStart)
                             };

            var q = from ns in nextStarts
                    join s in _db.TblShowtimes.AsNoTracking()
                        on new { MovieId = ns.MovieId, NextStart = ns.NextStart }
                        equals new { MovieId = s.ShowtimeMovieId, NextStart = s.ShowtimeStart }
                    join m in _db.TblMovies.AsNoTracking() on ns.MovieId equals m.MovieId
                    join scr in _db.TblScreens.AsNoTracking() on s.ShowtimeScreenId equals scr.ScreenId
                    join c in _db.TblCinemas.AsNoTracking() on scr.ScreenCinemaId equals c.CinemaId
                    where (m.MovieStartDate == null || m.MovieStartDate <= now)
                          && (m.MovieEndDate == null || m.MovieEndDate >= now)
                    orderby s.ShowtimeStart
                    select new
                    {
                        MovieId = m.MovieId,
                        MovieTitle = m.MovieTitle,
                        MovieDescription = m.MovieDescription,
                        MovieDurationMin = m.MovieDurationMin,
                        ShowtimeStart = s.ShowtimeStart,
                        ShowtimePrice = s.ShowtimePrice,
                        ShowtimeId = s.ShowtimeId,
                        PosterUrl = m.MovieImg,
                        CinemaName = c.CinemaName,
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
                PosterUrl = string.IsNullOrWhiteSpace(x.PosterUrl) ? "/images/movie-placeholder.png" : x.PosterUrl
            }).ToList();
        }

        public async Task<MovieDetailsVm> GetMovieDetailsAsync(int movieId)
        {
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

            movie.Comments = await _db.TblCustomerComplaints
                .AsNoTracking()
                .Where(c => c.CustomerComplaintMovieId == movieId && c.CustomerComplaintStatus == 1)
                .OrderByDescending(c => c.CustomerComplaintCreatedAt)
                .Select(c => new CommentVm
                {
                    Id = c.CustomerComplaintId,
                    UserId = c.CustomerComplaintCustomerUserId,
                    UserName = c.CustomerComplaintCustomerUser != null ? c.CustomerComplaintCustomerUser.UsersFullName : "Khách",
                    Rate = c.CustomerComplaintRate,
                    Text = c.CustomerComplaintDescription,
                    CreatedAt = c.CustomerComplaintCreatedAt
                }).ToListAsync();

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
                CustomerComplaintStatus = 0,
                CustomerComplaintCreatedAt = DateTime.Now
            };

            _db.TblCustomerComplaints.Add(ent);
            await _db.SaveChangesAsync();
        }

        // ==================== COUPON ====================

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

        // ==================== CINEMA SETTINGS (ADMIN) ====================

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
    }
}
