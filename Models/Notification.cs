using System;
using System.Collections.Generic;

namespace WebsiteBaby3.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public string Content { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? TargetRole { get; set; }

    public int? UserId { get; set; }
}
