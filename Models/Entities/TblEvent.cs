using System;
using System.Collections.Generic;


namespace Semester03.Models.Entities
{
    public partial class TblEvent
    {
        public TblEvent()
        {
            TblEventBookings = new HashSet<TblEventBooking>();
        }


        public int EventId { get; set; }
        public string EventName { get; set; }
        public string EventImg { get; set; }
        public string EventDescription { get; set; }
        public DateTime EventStart { get; set; }
        public DateTime EventEnd { get; set; }
        public int? EventStatus { get; set; }
        public int EventMaxSlot { get; set; }
        public int EventTenantPositionId { get; set; }


        public virtual TblTenantPosition EventTenantPosition { get; set; }
        public virtual ICollection<TblEventBooking> TblEventBookings { get; set; }
    }
}