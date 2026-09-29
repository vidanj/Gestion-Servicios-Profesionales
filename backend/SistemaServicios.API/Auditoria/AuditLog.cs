using System;

namespace SistemaServicios.API.Auditoria;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? UserId { get; set; }

    public string? UserEmail { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? ChangedColumns { get; set; }

    public string? IpAddress { get; set; }

    public string? TraceId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
