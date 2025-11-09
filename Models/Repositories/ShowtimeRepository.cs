using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;

namespace Semester03.Areas.Client.Repositories
{
    public class ShowtimeRepository
    {
        private readonly AbcdmallContext _db;
        public ShowtimeRepository(AbcdmallContext db) => _db = db;

        public List<ShowtimeVm> GetShowtimesForMovieOnDate(int movieId, DateTime localDate)
        {
            var start = localDate.Date;
            var end = start.AddDays(1);

            var q = _db.TblShowtimes
                       .AsNoTracking()
                       .Include(s => s.ShowtimeScreen)
                           .ThenInclude(sc => sc.ScreenCinema)
                       .Where(s => s.ShowtimeMovieId == movieId
                                   && s.ShowtimeStart >= start
                                   && s.ShowtimeStart < end)
                       .OrderBy(s => s.ShowtimeStart)
                       .Select(s => new ShowtimeVm
                       {
                           Id = s.ShowtimeId,
                           StartTime = s.ShowtimeStart,
                           Price = s.ShowtimePrice,
                           ScreenName = s.ShowtimeScreen != null ? s.ShowtimeScreen.ScreenName : "",
                           CinemaName = s.ShowtimeScreen != null && s.ShowtimeScreen.ScreenCinema != null
                                        ? s.ShowtimeScreen.ScreenCinema.CinemaName : ""
                       });

            return q.ToList();
        }

        public IEnumerable<DateTime> GetAvailableDatesForMovie(int movieId, DateTime fromDate, int days)
        {
            var end = fromDate.Date.AddDays(days);
            var q = _db.TblShowtimes
                       .AsNoTracking()
                       .Where(s => s.ShowtimeMovieId == movieId
                                   && s.ShowtimeStart >= fromDate.Date
                                   && s.ShowtimeStart < end)
                       .Select(s => s.ShowtimeStart.Date)
                       .Distinct()
                       .OrderBy(d => d);

            var result = new List<DateTime>();
            for (int i = 0; i < days; i++)
                result.Add(fromDate.Date.AddDays(i));

            return result;
        }
    }
}
