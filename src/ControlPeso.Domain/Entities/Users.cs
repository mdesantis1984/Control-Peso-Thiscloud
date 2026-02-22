using System;
using System.Collections.Generic;

namespace ControlPeso.Infrastructure;

public partial class Users
{
    public string Id { get; set; } = null!;

    public string? GoogleId { get; set; }

    public string? LinkedInId { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int Role { get; set; }

    public string? AvatarUrl { get; set; }

    public string MemberSince { get; set; } = null!;

    public double Height { get; set; }

    public int UnitSystem { get; set; }

    public string? DateOfBirth { get; set; }

    public string Language { get; set; } = null!;

    public int Status { get; set; }

    public double? GoalWeight { get; set; }

    public double? StartingWeight { get; set; }

    public string CreatedAt { get; set; } = null!;

    public string UpdatedAt { get; set; } = null!;

    public virtual ICollection<AuditLog> AuditLog { get; set; } = new List<AuditLog>();

    public virtual ICollection<UserNotifications> UserNotifications { get; set; } = new List<UserNotifications>();

    public virtual UserPreferences? UserPreferences { get; set; }

    public virtual ICollection<WeightLogs> WeightLogs { get; set; } = new List<WeightLogs>();
}
