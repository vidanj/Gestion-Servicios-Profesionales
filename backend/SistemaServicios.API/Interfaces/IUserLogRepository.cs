using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces;

public interface IUserLogRepository
{
    public Task<(IEnumerable<UserLog> logs, int totalCount)> GetLogsAsync(
        int page,
        int size,
        LogStatus? status,
        Guid? userId,
        LogAction? action
    );

    public Task<UserLog> AddLogAsync(UserLog log);
}
