using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.DTOs.Requests;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Repositories;

public class ServiceRequestRepository : IServiceRequestRepository
{
    private static readonly Expression<Func<Request, ServiceRequestDto>> ToDto =
        r => new ServiceRequestDto
        {
            Id = r.Id,
            ClientId = r.ClientId,
            ClientName =
                r.Client != null ? r.Client.FirstName + " " + r.Client.LastName : string.Empty,
            ProfessionalId = r.ProfessionalId,
            ProfessionalName =
                r.Professional != null
                    ? r.Professional.FirstName + " " + r.Professional.LastName
                    : string.Empty,
            ServiceId = r.ServiceId,
            ServiceTitle = r.Service != null ? r.Service.Title : string.Empty,
            QuotedPrice = r.QuotedPrice,
            Status = r.Status,
            Description = r.Description,
            RequestDate = r.RequestDate,
            ScheduledDate = r.ScheduledDate,
            CompletionDate = r.CompletionDate,
        };

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

    public async Task<Request?> GetByIdAsync(int id) =>
        await _context.Requests.FirstOrDefaultAsync(r => r.Id == id);

    public async Task UpdateAsync(Request request)
    {
        _context.Requests.Update(request);
        await _context.SaveChangesAsync();
    }

    public async Task<ServiceRequestDto?> GetRequestDtoByIdAsync(int id) =>
        await _context.Requests.Where(r => r.Id == id).Select(ToDto).FirstOrDefaultAsync();

    public async Task<(
        IEnumerable<ServiceRequestDto> requests,
        int totalCount
    )> GetDtosByClientIdAsync(Guid clientId, int page, int size)
    {
        var query = _context
            .Requests.Where(r => r.ClientId == clientId)
            .OrderByDescending(r => r.RequestDate);

        var totalCount = await query.CountAsync();
        var requests = await query.Skip((page - 1) * size).Take(size).Select(ToDto).ToListAsync();

        return (requests, totalCount);
    }

    public async Task<(
        IEnumerable<ServiceRequestDto> requests,
        int totalCount
    )> GetDtosByProfessionalIdAsync(Guid professionalId, int page, int size)
    {
        var query = _context
            .Requests.Where(r => r.ProfessionalId == professionalId)
            .OrderByDescending(r => r.RequestDate);

        var totalCount = await query.CountAsync();
        var requests = await query.Skip((page - 1) * size).Take(size).Select(ToDto).ToListAsync();

        return (requests, totalCount);
    }
}
