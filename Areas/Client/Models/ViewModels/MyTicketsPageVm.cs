using System.Collections.Generic;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class MyTicketsPageVm
    {
        public List<MyTicketVm> MovieTickets { get; set; } = new();
        public List<MyEventTicketVm> EventTickets { get; set; } = new();
    }
}
