namespace Semester03.Areas.Partner.Models
{
    public class EventBooking
    {
        public int Id { get; set; } = 0;
        public int TenantId { get; set; } = 0;
        public int UserId { get; set; } = 0;
        public int EventId {  get; set; } = 0;
        public DateOnly Date { get; set; }
        public int Quantity { get; set; } = 0;
        public decimal UnitPrice {  get; set; } = 0;
        public decimal TotalCost { get; set; } = 0;
        public int PaymentStatus { get; set; } = 0;
        public int Status { get; set; } = 0;
        public string Note { get; set; } = "";
        public DateTime Created { get; set; }
        public string Username { get; set; } = "";
        public string TenantName { get; set; } = "";
        public string EventName { get; set; } = "";
    }
}
