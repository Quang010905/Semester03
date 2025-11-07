using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Semester03.Models.Entities; // scaffolded context namespace
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;

namespace Semester03.Areas.Client.Repositories
{
    public class CinemaRepository : ICinemaRepository
    {
        private readonly AbcdmallContext _db;
        public CinemaRepository(AbcdmallContext db)
        {
            _db = db;
        }

        public async Task<List<MovieCardVm>> GetFeaturedMoviesAsync(int top = 5)
        {
            var now = DateTime.Now;

            // 1) For each movie, get the earliest upcoming showtime start (NextStart)
            var nextStarts = _db.TblShowtimes
                .AsNoTracking()
                .Where(s => s.ShowtimeStart >= now)
                .GroupBy(s => s.ShowtimeMovieId)
                .Select(g => new
                {
                    MovieId = g.Key,
                    NextStart = g.Min(s => s.ShowtimeStart)
                });

            // 2) Join nextStarts back to showtimes to obtain the actual showtime row, then join movie/screen/cinema
            var q = from ns in nextStarts
                    join s in _db.TblShowtimes.AsNoTracking()
                        on new { MovieId = ns.MovieId, NextStart = ns.NextStart }
                        equals new { MovieId = s.ShowtimeMovieId, NextStart = s.ShowtimeStart }
                    join m in _db.TblMovies.AsNoTracking() on ns.MovieId equals m.MovieId
                    join scr in _db.TblScreens.AsNoTracking() on s.ShowtimeScreenId equals scr.ScreenId
                    join c in _db.TblCinemas.AsNoTracking() on scr.ScreenCinemaId equals c.CinemaId
                    orderby s.ShowtimeStart
                    select new
                    {
                        Movie = m,
                        Showtime = s,
                        Screen = scr,
                        Cinema = c
                    };

            var list = await q.Take(top).ToListAsync();

            var result = list.Select(x => new MovieCardVm
            {
                Id = x.Movie.MovieId,
                Title = x.Movie.MovieTitle,
                Description = x.Movie.MovieDescription ?? "",
                DurationMin = x.Movie.MovieDurationMin,
                NextShowtime = x.Showtime.ShowtimeStart,
                NextPrice = x.Showtime.ShowtimePrice,
                NextShowtimeId = x.Showtime.ShowtimeId,
                PosterUrl = "/images/movie-placeholder.png"
            }).ToList();

            return result;
        }

        public async Task<List<MovieCardVm>> GetNowShowingAsync()
        {
            var now = DateTime.Now;

            var nextStarts = _db.TblShowtimes
                .AsNoTracking()
                .Where(s => s.ShowtimeStart >= now)
                .GroupBy(s => s.ShowtimeMovieId)
                .Select(g => new
                {
                    MovieId = g.Key,
                    NextStart = g.Min(s => s.ShowtimeStart)
                });

            var q = from ns in nextStarts
                    join s in _db.TblShowtimes.AsNoTracking()
                        on new { MovieId = ns.MovieId, NextStart = ns.NextStart }
                        equals new { MovieId = s.ShowtimeMovieId, NextStart = s.ShowtimeStart }
                    join m in _db.TblMovies.AsNoTracking() on ns.MovieId equals m.MovieId
                    join scr in _db.TblScreens.AsNoTracking() on s.ShowtimeScreenId equals scr.ScreenId
                    join c in _db.TblCinemas.AsNoTracking() on scr.ScreenCinemaId equals c.CinemaId
                    orderby s.ShowtimeStart
                    select new
                    {
                        Movie = m,
                        Showtime = s,
                        Screen = scr,
                        Cinema = c
                    };

            var list = await q.ToListAsync();

            var result = list.Select(x => new MovieCardVm
            {
                Id = x.Movie.MovieId,
                Title = x.Movie.MovieTitle,
                Description = x.Movie.MovieDescription ?? "",
                DurationMin = x.Movie.MovieDurationMin,
                NextShowtime = x.Showtime.ShowtimeStart,
                NextPrice = x.Showtime.ShowtimePrice,
                NextShowtimeId = x.Showtime.ShowtimeId,
                PosterUrl = "/images/movie-placeholder.png"
            }).ToList();

            return result;
        }

        public async Task<List<ShowtimeVm>> GetShowtimesByMovieAsync(int movieId, DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var q = from s in _db.TblShowtimes.AsNoTracking()
                    where s.ShowtimeMovieId == movieId
                          && s.ShowtimeStart >= startDate
                          && s.ShowtimeStart < endDate
                    join scr in _db.TblScreens.AsNoTracking() on s.ShowtimeScreenId equals scr.ScreenId
                    join c in _db.TblCinemas.AsNoTracking() on scr.ScreenCinemaId equals c.CinemaId
                    orderby s.ShowtimeStart
                    select new ShowtimeVm
                    {
                        Id = s.ShowtimeId,
                        CinemaName = c.CinemaName,
                        ScreenName = scr.ScreenName,
                        StartTime = s.ShowtimeStart,
                        Price = s.ShowtimePrice
                    };

            return await q.ToListAsync();
        }
    }
}
