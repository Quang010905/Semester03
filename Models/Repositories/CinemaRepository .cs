using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;

namespace Semester03.Areas.Client.Repositories
{
    public class CinemaRepository
    {
        private readonly AbcdmallContext _db;
        public CinemaRepository(AbcdmallContext db) => _db = db;

        // Get top featured (by earliest upcoming showtime) - project only required columns
        public async Task<List<MovieCardVm>> GetFeaturedMoviesAsync(int top = 3)
        {
            var now = DateTime.UtcNow;

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
            var now = DateTime.UtcNow;

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
    }
}
