using System;
using System.Collections.Generic;

namespace ControlPeso.Domain.Entities;

public partial class WeightLogs
{
    public string Id { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string Date { get; set; } = null!;

    public string Time { get; set; } = null!;

    public double Weight { get; set; }

    public int DisplayUnit { get; set; }

    public string? Note { get; set; }

    public int Trend { get; set; }

    public string CreatedAt { get; set; } = null!;

    public virtual Users User { get; set; } = null!;
}
