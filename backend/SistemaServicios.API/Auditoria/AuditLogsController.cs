using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;

namespace SistemaServicios.API.Auditoria;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogsController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? entityName,
        [FromQuery] string? traceId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(entityName))
        {
            query = query.Where(a => a.EntityName == entityName);
        }

        if (!string.IsNullOrEmpty(traceId))
        {
            query = query.Where(a => a.TraceId == traceId);
        }

        if (from.HasValue)
        {
            var desde = from.Value.UtcDateTime;
            query = query.Where(a => a.CreatedAtUtc >= desde);
        }

        if (to.HasValue)
        {
            var hasta = to.Value.UtcDateTime;
            query = query.Where(a => a.CreatedAtUtc <= hasta);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id,
                a.UserId,
                a.UserEmail,
                a.Action,
                a.EntityName,
                a.EntityId,
                a.OldValues,
                a.NewValues,
                a.ChangedColumns,
                a.IpAddress,
                a.TraceId,
                a.CreatedAtUtc
            ))
            .ToListAsync();

        return Ok(
            new
            {
                total,
                page,
                pageSize,
                items,
            }
        );
    }
}
