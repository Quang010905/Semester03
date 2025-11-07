using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Semester03.Areas.Client.Repositories
{
    public class MovieRepository : IMovieRepository
    {
        private readonly AbcdmallContext _db;
        public MovieRepository(AbcdmallContext db) => _db = db;

        public MovieCardVm? GetMovieCard(int movieId)
        {
            var m = _db.TblMovies
                       .AsNoTracking()
                       .FirstOrDefault(x => x.MovieId == movieId);

            if (m == null) return null;

            // try to get next showtime & price (optional)
            var next = _db.TblShowtimes
                          .AsNoTracking()
                          .Where(s => s.ShowtimeMovieId == movieId && s.ShowtimeStart >= DateTime.UtcNow)
                          .OrderBy(s => s.ShowtimeStart)
                          .Select(s => new { s.ShowtimeId, s.ShowtimeStart, s.ShowtimePrice })
                          .FirstOrDefault();

            return new MovieCardVm
            {
                Id = m.MovieId,
                Title = m.MovieTitle ?? "",
                DurationMin = m.MovieDurationMin,
                Description = m.MovieDescription ?? "",
                NextShowtime = next?.ShowtimeStart,
                NextPrice = next?.ShowtimePrice,
                PosterUrl = "/images/movie-placeholder.png", // can be extended if you have poster column
                NextShowtimeId = next?.ShowtimeId
            };
        }
    }
}
