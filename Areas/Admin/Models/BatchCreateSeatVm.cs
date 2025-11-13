namespace Semester03.Areas.Admin.Models
{
    public class BatchCreateSeatVm
    {
        public int ScreenId { get; set; }
        public int NumRows { get; set; } // e.g., 6 (for A-F)
        public int NumCols { get; set; } // e.g., 10 (for 1-10)
    }
}
