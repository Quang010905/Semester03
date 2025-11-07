using Semester03.Areas.Client.Models.ViewModels;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Repositories
{
    public interface ISeatRepository
    {
        SelectSeatVm GetSeatLayoutForShowtime(int showtimeId);
        (List<int> succeeded, List<int> failed) ReserveSeats(int showtimeId, List<int> showtimeSeatIds, int? userId);
        SelectSeatVm RefreshSeatLayout(int showtimeId); // useful to refresh after reserve
    }
}
