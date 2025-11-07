using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Semester03.Areas.Client.Models.ViewModels;

namespace Semester03.Areas.Client.Repositories
{
    public interface ICinemaRepository
    {
        Task<List<MovieCardVm>> GetFeaturedMoviesAsync(int top = 3);
        Task<List<MovieCardVm>> GetNowShowingAsync();
        Task<List<ShowtimeVm>> GetShowtimesByMovieAsync(int movieId, DateTime date);
    }
}
