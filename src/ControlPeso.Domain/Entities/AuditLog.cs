using System;
using System.Collections.Generic;

namespace ControlPeso.Infrastructure;

public partial class AuditLog
{
    public string Id { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string CreatedAt { get; set; } = null!;

    public virtual Users User { get; set; } = null!;
}
