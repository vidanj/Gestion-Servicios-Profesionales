using SistemaServicios.API.DTOs;
using SistemaServicios.API.DTOs.Services;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Services;

public class ServiceService : IServiceService
{
    private readonly IServiceRepository _serviceRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUserLogService _userLogService;

    public ServiceService(
        IServiceRepository serviceRepository,
        ICategoryRepository categoryRepository,
        IUserLogService userLogService
    )
    {
        _serviceRepository = serviceRepository;
        _categoryRepository = categoryRepository;
        _userLogService = userLogService;
    }

    public async Task<(IEnumerable<ServiceDto> services, int totalCount)> GetServicesAsync(
        int page,
        int size,
        int? categoryId,
        Guid? professionalId
    )
    {
        var (services, totalCount) = await _serviceRepository.GetAllAsync(
            page,
            size,
            categoryId,
            professionalId
        );
        return (services.Select(MapToDto), totalCount);
    }

    public async Task<ServiceDto> GetServiceByIdAsync(int id)
    {
        var service = await _serviceRepository.GetByIdAsync(id);
        if (service is null)
        {
            throw new KeyNotFoundException($"Servicio con ID {id} no encontrado.");
        }

        return MapToDto(service);
    }

    public async Task<(IEnumerable<ServiceDto> services, int totalCount)> GetMyServicesAsync(
        Guid professionalId,
        int page,
        int size
    )
    {
        var (services, totalCount) = await _serviceRepository.GetByProfessionalIdAsync(
            professionalId,
            page,
            size
        );
        return (services.Select(MapToDto), totalCount);
    }

    public async Task<ServiceDto> CreateServiceAsync(Guid professionalId, CreateServiceDto dto)
    {
        var categoryExists = await _categoryRepository.ExistsAsync(
            dto.CategoryId,
            mustBeActive: true
        );
        if (!categoryExists)
        {
            throw new KeyNotFoundException(
                $"La categoría con ID {dto.CategoryId} no existe o está inactiva."
            );
        }

        var service = new Service
        {
            ProfessionalId = professionalId,
            CategoryId = dto.CategoryId,
            Title = dto.Title,
            Description = dto.Description,
            BasePrice = dto.BasePrice,
            ImageUrl = dto.ImageUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var created = await _serviceRepository.CreateAsync(service);

        await _userLogService.CreateLogAsync(
            new CreateUserLogDto
            {
                UserId = professionalId,
                Action = LogAction.CreacionServicio,
                Detail = $"Servicio creado: '{created.Title}' (ID: {created.Id})",
                Status = LogStatus.Exitoso,
            }
        );

        var withIncludes = await _serviceRepository.GetByIdAsync(created.Id);
        return MapToDto(withIncludes!);
    }

    public async Task<ServiceDto> UpdateServiceAsync(
        int id,
        Guid requesterId,
        string requesterRole,
        UpdateServiceDto dto
    )
    {
        var service = await _serviceRepository.GetByIdAsync(id);
        if (service is null)
        {
            throw new KeyNotFoundException($"Servicio con ID {id} no encontrado.");
        }

        if (requesterRole != "Admin" && service.ProfessionalId != requesterId)
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para modificar este servicio."
            );
        }

        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await _categoryRepository.ExistsAsync(
                dto.CategoryId.Value,
                mustBeActive: true
            );
            if (!categoryExists)
            {
                throw new KeyNotFoundException(
                    $"La categoría con ID {dto.CategoryId.Value} no existe o está inactiva."
                );
            }

            service.CategoryId = dto.CategoryId.Value;
        }

        if (dto.Title is not null)
        {
            service.Title = dto.Title;
        }

        if (dto.Description is not null)
        {
            service.Description = dto.Description;
        }

        if (dto.BasePrice.HasValue)
        {
            service.BasePrice = dto.BasePrice.Value;
        }

        if (dto.ImageUrl is not null)
        {
            service.ImageUrl = dto.ImageUrl;
        }

        service.UpdatedAt = DateTime.UtcNow;

        var updated = await _serviceRepository.UpdateAsync(service);

        await _userLogService.CreateLogAsync(
            new CreateUserLogDto
            {
                UserId = requesterId,
                Action = LogAction.ActualizacionServicio,
                Detail = $"Servicio actualizado: '{updated.Title}' (ID: {updated.Id})",
                Status = LogStatus.Exitoso,
            }
        );

        var withIncludes = await _serviceRepository.GetByIdAsync(updated.Id);
        return MapToDto(withIncludes!);
    }

    public async Task<ServiceDto> ToggleActiveAsync(int id, Guid requesterId, string requesterRole)
    {
        var service = await _serviceRepository.GetByIdAsync(id);
        if (service is null)
        {
            throw new KeyNotFoundException($"Servicio con ID {id} no encontrado.");
        }

        if (requesterRole != "Admin" && service.ProfessionalId != requesterId)
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para modificar este servicio."
            );
        }

        service.IsActive = !service.IsActive;
        service.UpdatedAt = DateTime.UtcNow;

        var updated = await _serviceRepository.UpdateAsync(service);

        await _userLogService.CreateLogAsync(
            new CreateUserLogDto
            {
                UserId = requesterId,
                Action = LogAction.ActualizacionServicio,
                Detail =
                    $"Servicio '{updated.Title}' (ID: {updated.Id}) "
                    + (updated.IsActive ? "activado" : "desactivado"),
                Status = LogStatus.Exitoso,
            }
        );

        var withIncludes = await _serviceRepository.GetByIdAsync(updated.Id);
        return MapToDto(withIncludes!);
    }

    public async Task DeleteServiceAsync(int id, Guid requesterId, string requesterRole)
    {
        var service = await _serviceRepository.GetByIdAsync(id);
        if (service is null)
        {
            throw new KeyNotFoundException($"Servicio con ID {id} no encontrado.");
        }

        if (requesterRole != "Admin" && service.ProfessionalId != requesterId)
        {
            throw new UnauthorizedAccessException(
                "No tienes permiso para eliminar este servicio."
            );
        }

        service.IsActive = false;
        service.UpdatedAt = DateTime.UtcNow;

        await _serviceRepository.UpdateAsync(service);

        await _userLogService.CreateLogAsync(
            new CreateUserLogDto
            {
                UserId = requesterId,
                Action = LogAction.EliminacionServicio,
                Detail = $"Servicio eliminado (desactivado): '{service.Title}' (ID: {service.Id})",
                Status = LogStatus.Exitoso,
            }
        );
    }

    private static ServiceDto MapToDto(Service s) =>
        new()
        {
            Id = s.Id,
            ProfessionalId = s.ProfessionalId,
            ProfessionalName =
                s.Professional is not null
                    ? $"{s.Professional.FirstName} {s.Professional.LastName}"
                    : string.Empty,
            CategoryId = s.CategoryId,
            CategoryName = s.Category?.Name ?? string.Empty,
            Title = s.Title,
            Description = s.Description,
            BasePrice = s.BasePrice,
            ImageUrl = s.ImageUrl,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
        };
}
