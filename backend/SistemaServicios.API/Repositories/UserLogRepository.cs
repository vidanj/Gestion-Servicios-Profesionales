using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Repositories;

public class UserLogRepository : IUserLogRepository
{
    private readonly AppDbContext _context;

    public UserLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<UserLog> logs, int totalCount)> GetLogsAsync(
        int page,
        int size,
        LogStatus? status,
        Guid? userId,
        LogAction? action)
    {
        var query = _context.UserLogs
            .Include(l => l.User)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(l => l.UserId == userId.Value);
        }

        if (action.HasValue)
        {
            query = query.Where(l => l.Action == action.Value);
        }

        var total = await query.CountAsync();
        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (logs, total);
    }

    public async Task<UserLog> AddLogAsync(UserLog log)
    {
        _context.UserLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }
}
