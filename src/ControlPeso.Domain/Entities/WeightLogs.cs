namespace ControlPeso.Domain.Entities;

public partial class WeightLogs
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly Time { get; set; }

    public decimal Weight { get; set; }

    public int DisplayUnit { get; set; }

    public string? Note { get; set; }

    public int Trend { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Users User { get; set; } = null!;
}
