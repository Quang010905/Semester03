using Semester03.Models.Entities;

namespace Semester03.Areas.Admin.Models
{
    public class ParkingLevelDetailViewModel
    {
        // 1. The Level (parent)
        public TblParkingLevel ParkingLevel { get; set; }

        // 2. The form for adding a NEW spot [cite: 101-116]
        public TblParkingSpot NewSpot { get; set; }

        // 3. The form for EDITING an existing spot [cite: 187-194]
        public TblParkingSpot SpotToEdit { get; set; }
    }
}
