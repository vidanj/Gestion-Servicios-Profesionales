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
        var log = new UserLog
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            Action = dto.Action,
            Detail = dto.Detail,
            Status = dto.Status,
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
            CreatedAt = l.CreatedAt,
        };
}
