// EventCardVm.cs
using System;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class EventCardVm
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string ImageUrl { get; set; } = "/images/event-placeholder.png";
        public int? MaxSlot { get; set; }
        public int Status { get; set; }
    }
}

// EventHomeVm.cs


namespace Semester03.Areas.Client.Models.ViewModels
{
    public class EventHomeVm
    {
        public List<EventCardVm> Featured { get; set; } = new();
        public List<EventCardVm> Upcoming { get; set; } = new();
    }
}

// EventDetailsVm.cs


namespace Semester03.Areas.Client.Models.ViewModels
{
    public class EventDetailsVm
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string ImageUrl { get; set; } = "/images/event-placeholder.png";
        public int? MaxSlot { get; set; }
        public int Status { get; set; }
        public int? TenantPositionId { get; set; }
    }
}
