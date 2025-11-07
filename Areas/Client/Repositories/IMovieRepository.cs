using Semester03.Areas.Client.Models.ViewModels;

namespace Semester03.Areas.Client.Repositories
{
    public interface IMovieRepository
    {
        MovieCardVm? GetMovieCard(int movieId);
    }
}
