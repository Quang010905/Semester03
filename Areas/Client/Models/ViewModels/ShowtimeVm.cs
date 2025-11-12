using System;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class ShowtimeVm
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public decimal? Price { get; set; }
        public string ScreenName { get; set; } = "";
        public string CinemaName { get; set; } = "";
    }
}
