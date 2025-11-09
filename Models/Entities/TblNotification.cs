using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities;

public partial class TblNotification
{
    public int NotificationId { get; set; }

    public int NotificationUserId { get; set; }

    public string NotificationTitle { get; set; } = null!;

    public string NotificationBody { get; set; } = null!;

    public string? NotificationChannel { get; set; }

    public bool? NotificationIsRead { get; set; }

    public DateTime? NotificationCreatedAt { get; set; }

    public virtual TblUser NotificationUser { get; set; } = null!;
}
