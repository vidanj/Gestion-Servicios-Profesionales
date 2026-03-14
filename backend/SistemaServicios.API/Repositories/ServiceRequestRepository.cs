using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Repositories;

public class ServiceRequestRepository : IServiceRequestRepository
{
    private readonly AppDbContext _context;

    public ServiceRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Request> CreateAsync(Request request)
    {
        _context.Requests.Add(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public async Task<Request?> GetByIdAsync(int id)
    {
        return await _context
            .Requests.Include(r => r.Client)
            .Include(r => r.Professional)
            .Include(r => r.Service)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<(IEnumerable<Request> requests, int totalCount)> GetByClientIdAsync(
        Guid clientId,
        int page,
        int size
    )
    {
        var query = _context
            .Requests.Include(r => r.Client)
            .Include(r => r.Professional)
            .Include(r => r.Service)
            .Where(r => r.ClientId == clientId)
            .OrderByDescending(r => r.RequestDate);

        var totalCount = await query.CountAsync();
        var requests = await query.Skip((page - 1) * size).Take(size).ToListAsync();

        return (requests, totalCount);
    }

    public async Task<(IEnumerable<Request> requests, int totalCount)> GetByProfessionalIdAsync(
        Guid professionalId,
        int page,
        int size
    )
    {
        var query = _context
            .Requests.Include(r => r.Client)
            .Include(r => r.Professional)
            .Include(r => r.Service)
            .Where(r => r.ProfessionalId == professionalId)
            .OrderByDescending(r => r.RequestDate);

        var totalCount = await query.CountAsync();
        var requests = await query.Skip((page - 1) * size).Take(size).ToListAsync();

        return (requests, totalCount);
    }

    public async Task UpdateAsync(Request request)
    {
        _context.Requests.Update(request);
        await _context.SaveChangesAsync();
    }
}
