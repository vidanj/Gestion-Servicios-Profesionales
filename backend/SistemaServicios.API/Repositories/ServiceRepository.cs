using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Repositories;

public class ServiceRepository : IServiceRepository
{
    private readonly AppDbContext _context;

    public ServiceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Service> services, int totalCount)> GetAllAsync(
        int page,
        int size,
        int? categoryId,
        Guid? professionalId
    )
    {
        var query = _context
            .Services.Include(s => s.Category)
            .Include(s => s.Professional)
            .Where(s => s.IsActive)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(s => s.CategoryId == categoryId.Value);
        }

        if (professionalId.HasValue)
        {
            query = query.Where(s => s.ProfessionalId == professionalId.Value);
        }

        var totalCount = await query.CountAsync();

        var services = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (services, totalCount);
    }

    public async Task<Service?> GetByIdAsync(int id)
    {
        return await _context
            .Services.Include(s => s.Category)
            .Include(s => s.Professional)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<(IEnumerable<Service> services, int totalCount)> GetByProfessionalIdAsync(
        Guid professionalId,
        int page,
        int size
    )
    {
        var query = _context
            .Services.Include(s => s.Category)
            .Include(s => s.Professional)
            .Where(s => s.ProfessionalId == professionalId)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var services = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (services, totalCount);
    }

    public async Task<Service> CreateAsync(Service service)
    {
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        return service;
    }

    public async Task<Service> UpdateAsync(Service service)
    {
        _context.Services.Update(service);
        await _context.SaveChangesAsync();
        return service;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Services.AnyAsync(s => s.Id == id);
    }
}
