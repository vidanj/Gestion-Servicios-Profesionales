using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SistemaServicios.API.Auditoria;

public class AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
    : SaveChangesInterceptor
{
    private static readonly string[] SensitiveWords = ["password", "hash", "token", "secret"];

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        AddAuditEntries(eventData.Context, httpContextAccessor.HttpContext);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        AddAuditEntries(eventData.Context, httpContextAccessor.HttpContext);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void AddAuditEntries(DbContext? context, HttpContext? httpContext)
    {
        if (context is null)
        {
            return;
        }

        var userIdText = GetClaim(httpContext, ClaimTypes.NameIdentifier, "sub");
        Guid? userId = Guid.TryParse(userIdText, out var parsedId) ? parsedId : null;
        var userEmail = GetClaim(httpContext, ClaimTypes.Email, "email");
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var traceId = Activity.Current?.TraceId.ToString();

        var entries = context
            .ChangeTracker.Entries()
            .Where(e =>
                e.Entity is not AuditLog
                && (e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            )
            .ToList();

        foreach (var entry in entries)
        {
            context
                .Set<AuditLog>()
                .Add(
                    new AuditLog
                    {
                        UserId = userId,
                        UserEmail = userEmail,
                        Action = entry.State switch
                        {
                            EntityState.Added => "CREATE",
                            EntityState.Modified => "UPDATE",
                            _ => "DELETE",
                        },
                        EntityName = entry.Entity.GetType().Name,
                        EntityId = GetEntityId(entry),
                        OldValues =
                            entry.State == EntityState.Added
                                ? null
                                : SerializeValues(entry, useOriginal: true),
                        NewValues =
                            entry.State == EntityState.Deleted
                                ? null
                                : SerializeValues(entry, useOriginal: false),
                        ChangedColumns = GetChangedColumns(entry),
                        IpAddress = ipAddress,
                        TraceId = traceId,
                        CreatedAtUtc = DateTime.UtcNow,
                    }
                );
        }
    }

    private static string? GetClaim(HttpContext? httpContext, params string[] claimTypes)
    {
        var user = httpContext?.User;
        if (user is null)
        {
            return null;
        }

        return claimTypes
            .Select(claimType => user.FindFirst(claimType)?.Value)
            .FirstOrDefault(value => !string.IsNullOrEmpty(value));
    }

    private static string GetEntityId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null)
        {
            return string.Empty;
        }

        return string.Join(
            ",",
            key.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "null")
        );
    }

    private static bool IsSensitive(string propertyName)
    {
        return SensitiveWords.Any(word =>
            propertyName.Contains(word, StringComparison.OrdinalIgnoreCase)
        );
    }

    private static string SerializeValues(EntityEntry entry, bool useOriginal)
    {
        var values = new Dictionary<string, object?>();
        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (IsSensitive(name))
            {
                continue;
            }

            values[name] = useOriginal ? property.OriginalValue : property.CurrentValue;
        }

        return JsonSerializer.Serialize(values);
    }

    private static string? GetChangedColumns(EntityEntry entry)
    {
        if (entry.State != EntityState.Modified)
        {
            return null;
        }

        var changed = entry
            .Properties.Where(p => p.IsModified)
            .Select(p => p.Metadata.Name)
            .ToList();

        return changed.Count > 0 ? JsonSerializer.Serialize(changed) : null;
    }
}
