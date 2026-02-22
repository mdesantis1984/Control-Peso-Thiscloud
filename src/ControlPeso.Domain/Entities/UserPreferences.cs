using System;
using System.Collections.Generic;

namespace ControlPeso.Infrastructure;

public partial class UserPreferences
{
    public string Id { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public int DarkMode { get; set; }

    public int NotificationsEnabled { get; set; }

    public string TimeZone { get; set; } = null!;

    public string UpdatedAt { get; set; } = null!;

    public virtual Users User { get; set; } = null!;
}
