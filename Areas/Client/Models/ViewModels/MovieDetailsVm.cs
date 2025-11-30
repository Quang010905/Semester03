namespace Semester03.Areas.Client.Models.ViewModels
{
    public class MovieDetailsVm
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public string Director { get; set; }
        public string PosterUrl { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Rate { get; set; }
        public int? DurationMin { get; set; }
        public string Description { get; set; }

        public List<CommentVm> Comments { get; set; } = new List<CommentVm>();
        public int CommentPageIndex { get; set; }
        public int CommentPageSize { get; set; }
        public int CommentTotalPages { get; set; }
        public int CommentCount { get; set; }
    }

    public class CommentVm
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; }
        public int Rate { get; set; }
        public string Text { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

}
