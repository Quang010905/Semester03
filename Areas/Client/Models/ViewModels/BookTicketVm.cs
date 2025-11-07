using System.Collections.Generic;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class BookTicketVm
    {
        public MovieCardVm? Movie { get; set; }
        public List<DayVm> WeekDays { get; set; } = new();
        public DateTime SelectedDate { get; set; }
    }

    public class DayVm
    {
        public DateTime Date { get; set; }
        public string Display { get; set; } = "";
    }
}
