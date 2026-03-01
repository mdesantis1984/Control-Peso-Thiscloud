namespace ControlPeso.Domain.Entities;

public partial class UserPreferences
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public bool DarkMode { get; set; }

    public bool NotificationsEnabled { get; set; }

    public string TimeZone { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }

    public virtual Users User { get; set; } = null!;
}
