using SistemaServicios.API.DTOs.Requests;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Services;

public class ServiceRequestService : IServiceRequestService
{
    private readonly IServiceRequestRepository _repository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IUserRepository _userRepository;

    public ServiceRequestService(
        IServiceRequestRepository repository,
        IServiceRepository serviceRepository,
        IUserRepository userRepository
    )
    {
        _repository = repository;
        _serviceRepository = serviceRepository;
        _userRepository = userRepository;
    }

    public async Task<Request> CreateRequestAsync(CreateServiceRequestDto requestDto)
    {
        var serviceExists = await _serviceRepository.ExistsAsync(requestDto.ServiceId);
        if (!serviceExists)
        {
            throw new KeyNotFoundException(
                $"El servicio con ID {requestDto.ServiceId} no existe."
            );
        }

        var client = await _userRepository.GetByIdAsync(requestDto.ClientId);
        if (client is null)
        {
            throw new KeyNotFoundException(
                $"El cliente con ID {requestDto.ClientId} no existe."
            );
        }

        var professional = await _userRepository.GetByIdAsync(requestDto.ProfessionalId);
        if (professional is null)
        {
            throw new KeyNotFoundException(
                $"El profesionista con ID {requestDto.ProfessionalId} no existe."
            );
        }

        var newRequest = new Request
        {
            ClientId = requestDto.ClientId,
            ProfessionalId = requestDto.ProfessionalId,
            ServiceId = requestDto.ServiceId,
            Description = requestDto.Description,
            Status = RequestStatus.Pending,
            RequestDate = DateTime.UtcNow,
        };

        var createdRequest = await _repository.CreateAsync(newRequest);
        return createdRequest;
    }

    public async Task<Request> UpdateStatusAsync(int requestId, RequestStatus newStatus)
    {
        var request =
            await _repository.GetByIdAsync(requestId)
            ?? throw new KeyNotFoundException("Solicitud no encontrada.");

        var oldStatus = request.Status;

        if (!IsValidTransition(oldStatus, newStatus))
        {
            throw new InvalidOperationException("Transición de estado inválida.");
        }

        request.Status = newStatus;

        if (newStatus == RequestStatus.Completed)
        {
            request.CompletionDate = DateTime.UtcNow;
        }

        await _repository.UpdateAsync(request);
        return request;
    }

    private static bool IsValidTransition(RequestStatus current, RequestStatus next)
    {
        if (current == RequestStatus.Pending)
        {
            return next == RequestStatus.Accepted || next == RequestStatus.Cancelled;
        }

        if (current == RequestStatus.Accepted)
        {
            return next == RequestStatus.InProgress || next == RequestStatus.Cancelled;
        }

        if (current == RequestStatus.InProgress)
        {
            return next == RequestStatus.Completed || next == RequestStatus.Cancelled;
        }

        return false;
    }
}
