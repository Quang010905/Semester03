namespace Semester03.Areas.Partner.Models
{
    public class ComPlaint
    {
        public int Id { get; set; } = 0;
        public int CustomerUserId { get; set; } = 0;
        public int TenantId { get; set; } = 0;
        public int Rate { get; set; } = 0;
        public string Description { get; set; } = "";
        public int Status {  get; set; } = 0;
        public DateTime Created { get; set; }
        public int MovieId { get; set; } = 0;
        public int EventId {get;set; } = 0;
        public string CustomerName { get; set; } = "";
    }
}
