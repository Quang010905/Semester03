using Semester03.Areas.Client.Models.ViewModels;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Repositories
{
    public interface ISeatRepository
    {
        (List<int> succeeded, List<int> failed) ReserveSeats(int showtimeId, List<int> showtimeSeatIds, int? userId);
        SelectSeatVm GetSeatLayoutForShowtime(int showtimeId);
        SelectSeatVm RefreshSeatLayout(int showtimeId);

        (List<int> succeeded, List<int> failed) FinalizeSeatsAtomic(int showtimeId, List<int> showtimeSeatIds, int? userId, decimal pricePerSeat);
    }
}
