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

    public async Task<ServiceRequest> CreateAsync(ServiceRequest request)
    {
        _context.ServiceRequests.Add(request);
        await _context.SaveChangesAsync();

        return request;
    }

    public async Task<ServiceRequest?> GetByIdAsync(int id)
    {
        return await _context.ServiceRequests.FindAsync(id);
    }

    public async Task<ServiceRequest> UpdateAsync(ServiceRequest request)
    {
        _context.ServiceRequests.Update(request);
        await _context.SaveChangesAsync();

        return request;
    }

    public async Task<RequestAuditLog> AddAuditLogAsync(RequestAuditLog auditLog)
    {
        _context.RequestAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        return auditLog;
    }
}
