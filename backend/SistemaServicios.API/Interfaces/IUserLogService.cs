using SistemaServicios.API.DTOs;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces;

public interface IUserLogService
{
    public Task<(IEnumerable<UserLogDto> logs, int totalCount)> GetLogsAsync(
        int page,
        int size,
        LogStatus? status,
        Guid? userId,
        LogAction? action
    );

    public Task<UserLogDto> CreateLogAsync(CreateUserLogDto dto);
}
