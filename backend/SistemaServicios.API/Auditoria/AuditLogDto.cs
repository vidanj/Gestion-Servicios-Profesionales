using System;

namespace SistemaServicios.API.Auditoria;

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string? UserEmail,
    string Action,
    string EntityName,
    string EntityId,
    string? OldValues,
    string? NewValues,
    string? ChangedColumns,
    string? IpAddress,
    string? TraceId,
    DateTime CreatedAtUtc
);
