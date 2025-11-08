using System;


namespace Semester03.Models.Entities
{
    public partial class TblNotification
    {
        public int NotificationId { get; set; }
        public int NotificationUserId { get; set; }
        public string NotificationTitle { get; set; }
        public string NotificationBody { get; set; }
        public string NotificationChannel { get; set; }
        public bool? NotificationIsRead { get; set; }
        public DateTime? NotificationCreatedAt { get; set; }


        public virtual TblUser NotificationUser { get; set; }
    }
}