using Semester03.Models.Entities;

namespace Semester03.Areas.Admin.Models
{
    public class ScreenDetailViewModel
    {
        // 1. The Screen itself (contains the List of TblSeats)
        public TblScreen Screen { get; set; }

        // 2. The model for the Batch Generator form
        public BatchCreateSeatVm BatchForm { get; set; }
    }
}
