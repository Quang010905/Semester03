using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;

namespace Semester03.Areas.Client.Repositories
{
    public class MovieRepository
    {
        private readonly AbcdmallContext _db;
        public MovieRepository(AbcdmallContext db) => _db = db;

        public MovieCardVm? GetMovieCard(int movieId)
        {
            var m = _db.TblMovies.AsNoTracking().FirstOrDefault(x => x.MovieId == movieId);
            if (m == null) return null;

            var next = _db.TblShowtimes.AsNoTracking()
                        .Where(s => s.ShowtimeMovieId == movieId && s.ShowtimeStart >= DateTime.UtcNow)
                        .OrderBy(s => s.ShowtimeStart)
                        .Select(s => new { s.ShowtimeId, s.ShowtimeStart, s.ShowtimePrice })
                        .FirstOrDefault();

            return new MovieCardVm
            {
                Id = m.MovieId,
                Title = m.MovieTitle ?? "",
                Description = m.MovieDescription ?? "",
                DurationMin = m.MovieDurationMin,
                NextShowtime = next?.ShowtimeStart,
                NextPrice = next?.ShowtimePrice,
                NextShowtimeId = next?.ShowtimeId,
                PosterUrl = string.IsNullOrWhiteSpace(m.MovieImg) ? "/images/movie-placeholder.png" : m.MovieImg
            };
        }
    }
}
