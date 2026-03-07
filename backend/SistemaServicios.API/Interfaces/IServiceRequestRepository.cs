using SistemaServicios.API.Models;

namespace SistemaServicios.API.Interfaces;

public interface IServiceRequestRepository
{
    public Task<ServiceRequest> CreateAsync(ServiceRequest request);

    public Task<ServiceRequest?> GetByIdAsync(int id);

    public Task<ServiceRequest> UpdateAsync(ServiceRequest request);

    public Task<RequestAuditLog> AddAuditLogAsync(RequestAuditLog auditLog);
}
