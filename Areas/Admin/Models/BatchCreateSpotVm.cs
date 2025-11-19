namespace Semester03.Areas.Admin.Models
{
    public class BatchCreateSpotVm
    {
        public int LevelId { get; set; } //
        public int NumRows { get; set; } // e.g., 10 (for A-J)
        public int NumCols { get; set; } // e.g., 15 (for 1-15)
    }
}
