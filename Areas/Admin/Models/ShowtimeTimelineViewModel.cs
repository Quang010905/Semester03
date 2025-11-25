using System;
using System.Collections.Generic;
using Semester03.Models.Entities;

namespace Semester03.Areas.Admin.Models
{
    public class ShowtimeTimelineViewModel
    {
        public DateTime SelectedDate { get; set; }
        public List<ScreenTimelineGroup> ScreenGroups { get; set; }
        public List<TblMovie> AvailableMovies { get; set; }
    }

    public class ScreenTimelineGroup
    {
        public int ScreenId { get; set; }
        public string ScreenName { get; set; }
        public int TotalSeats { get; set; }
        public List<TblShowtime> Showtimes { get; set; }
    }
}