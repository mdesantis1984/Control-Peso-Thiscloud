namespace ControlPeso.Domain.Entities;

public partial class Users
{
    public Guid Id { get; set; }

    public string? GoogleId { get; set; }

    public string? LinkedInId { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int Role { get; set; }

    public string? AvatarUrl { get; set; }

    public DateTime MemberSince { get; set; }

    public decimal Height { get; set; }

    public int UnitSystem { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string Language { get; set; } = null!;

    public int Status { get; set; }

    public decimal? GoalWeight { get; set; }

    public decimal? StartingWeight { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLog { get; set; } = new List<AuditLog>();

    public virtual ICollection<UserNotifications> UserNotifications { get; set; } = new List<UserNotifications>();

    public virtual UserPreferences? UserPreferences { get; set; }

    public virtual ICollection<WeightLogs> WeightLogs { get; set; } = new List<WeightLogs>();
}
