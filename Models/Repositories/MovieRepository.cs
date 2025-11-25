using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;

namespace Semester03.Models.Repositories
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
                        .Where(s => s.ShowtimeMovieId == movieId && s.ShowtimeStart >= DateTime.Now)
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

        //============== ADMIN REPO METHODS ==============

        public async Task<IEnumerable<TblMovie>> GetAllAsync()
        {
            return await _db.TblMovies.ToListAsync();
        }

        public async Task<TblMovie> GetByIdAsync(int id)
        {
            return await _db.TblMovies.FindAsync(id);
        }

        public async Task<TblMovie> AddAsync(TblMovie entity)
        {
            _db.TblMovies.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(TblMovie entity)
        {
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _db.TblMovies.FindAsync(id);
            if (entity != null)
            {
                _db.TblMovies.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<bool> UpdateStatusAsync(int id, int newStatus)
        {
            var movie = await _db.TblMovies.FindAsync(id);
            if (movie == null) return false;

            movie.MovieStatus = newStatus;
            _db.Entry(movie).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
