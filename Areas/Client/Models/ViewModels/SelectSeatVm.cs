using System;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class SelectSeatVm
    {
        public int ShowtimeId { get; set; }
        public string MovieTitle { get; set; } = "";
        public string ScreenName { get; set; } = "";
        public DateTime ShowtimeStart { get; set; }
        public List<SeatVm> Seats { get; set; } = new();
        public int MaxCols { get; set; } = 10;
    }

    public class SeatVm
    {
        public int ShowtimeSeatId { get; set; }
        public string Label { get; set; } = "";
        public string Row { get; set; } = "";
        public int Col { get; set; }
        public string Status { get; set; } = "available";
        public int? ReservedByUserId { get; set; }
        public DateTime? ReservedAt { get; set; }
    }


    public class ReserveRequestVm
    {
        public int ShowtimeId { get; set; }
        public List<int> ShowtimeSeatIds { get; set; } = new();
    }
}
