using System.Collections.Generic;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class CinemaHomeVm
    {
        public List<MovieCardVm> Featured { get; set; } = new();
        public List<MovieCardVm> NowShowing { get; set; } = new();
    }
}
