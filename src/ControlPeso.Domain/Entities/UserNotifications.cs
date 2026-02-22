using System;
using System.Collections.Generic;

namespace ControlPeso.Infrastructure;

public partial class UserNotifications
{
    public string Id { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public int Type { get; set; }

    public string? Title { get; set; }

    public string Message { get; set; } = null!;

    public int IsRead { get; set; }

    public string CreatedAt { get; set; } = null!;

    public string? ReadAt { get; set; }

    public virtual Users User { get; set; } = null!;
}
