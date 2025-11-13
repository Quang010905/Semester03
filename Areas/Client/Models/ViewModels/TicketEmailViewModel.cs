using System;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class TicketEmailViewModel
    {
        public string UserFullName { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public List<TicketEmailItem> Tickets { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public int PointsAwarded { get; set; }
        public DateTime PurchaseDate { get; set; }
    }
}
