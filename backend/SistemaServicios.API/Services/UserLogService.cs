using System.Diagnostics;
using SistemaServicios.API.DTOs;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Services;

public class UserLogService : IUserLogService
{
    private readonly IUserLogRepository _repo;

    public UserLogService(IUserLogRepository repo)
    {
        _repo = repo;
    }

    public async Task<(IEnumerable<UserLogDto> logs, int totalCount)> GetLogsAsync(
        int page,
        int size,
        LogStatus? status,
        Guid? userId,
        LogAction? action
    )
    {
        var (logs, total) = await _repo.GetLogsAsync(page, size, status, userId, action);
        return (logs.Select(MapToDto), total);
    }

    public async Task<UserLogDto> CreateLogAsync(CreateUserLogDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var log = new UserLog
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            Action = dto.Action,
            Detail = dto.Detail,
            Status = dto.Status,

            // Se toma de la petición en curso y nunca del DTO. Aceptarlo del cliente
            // permitiría apuntar una entrada de la bitácora a la traza de otra petición,
            // y el enlace entre ambas bitácoras dejaría de ser fiable justo cuando más
            // falta hace. Fuera de una petición no hay traza y queda nulo.
            TraceId = Activity.Current?.TraceId.ToString(),
            CreatedAt = DateTime.UtcNow,
        };

        var saved = await _repo.AddLogAsync(log);
        return MapToDto(saved);
    }

    private static UserLogDto MapToDto(UserLog l) =>
        new()
        {
            Id = l.Id,
            UserId = l.UserId,
            UserName = l.User != null ? $"{l.User.FirstName} {l.User.LastName}" : "Desconocido",
            Action = l.Action,
            Detail = l.Detail,
            Status = l.Status,
            TraceId = l.TraceId,
            CreatedAt = l.CreatedAt,
        };
}
