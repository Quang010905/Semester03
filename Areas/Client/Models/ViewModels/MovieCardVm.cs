using System;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class MovieCardVm
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int? DurationMin { get; set; }
        public DateTime? NextShowtime { get; set; }
        public decimal? NextPrice { get; set; }
        public int? NextShowtimeId { get; set; }
        public string PosterUrl { get; set; } = "/images/movie-placeholder.png";
    }
}
