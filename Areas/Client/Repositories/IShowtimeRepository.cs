using Semester03.Areas.Client.Models.ViewModels;

namespace Semester03.Areas.Client.Repositories
{
    public interface IShowtimeRepository
    {
        List<ShowtimeVm> GetShowtimesForMovieOnDate(int movieId, DateTime localDate);
        IEnumerable<DateTime> GetAvailableDatesForMovie(int movieId, DateTime fromDate, int days);
    }
}
