using SistemaServicios.API.DTOs.Services;

namespace SistemaServicios.API.Interfaces;

public interface IServiceService
{
    public Task<(IEnumerable<ServiceDto> services, int totalCount)> GetServicesAsync(
        int page,
        int size,
        int? categoryId,
        Guid? professionalId
    );

    public Task<ServiceDto> GetServiceByIdAsync(int id);

    public Task<(IEnumerable<ServiceDto> services, int totalCount)> GetMyServicesAsync(
        Guid professionalId,
        int page,
        int size
    );

    public Task<ServiceDto> CreateServiceAsync(Guid professionalId, CreateServiceDto dto);

    public Task<ServiceDto> UpdateServiceAsync(
        int id,
        Guid requesterId,
        string requesterRole,
        UpdateServiceDto dto
    );

    public Task<ServiceDto> ToggleActiveAsync(int id, Guid requesterId, string requesterRole);

    public Task DeleteServiceAsync(int id, Guid requesterId, string requesterRole);
}
