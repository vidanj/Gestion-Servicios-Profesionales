using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.Enums;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Services
{
    public class RequestService
    {
        private readonly AppDbContext _context;

        public RequestService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceRequest> CreateRequestAsync(int serviceId, Guid clientId, string message)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null) throw new KeyNotFoundException("El servicio no existe.");

            var request = new ServiceRequest
            {
                ServiceId = serviceId,
                ClientId = clientId,
                ClientMessage = message,
                Status = SistemaServicios.API.Enums.RequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.ServiceRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<IEnumerable<ServiceRequest>> GetRequestsForProfessionalAsync(Guid professionalId)
        {
            return await _context.ServiceRequests
                .Include(r => r.Client)
                .Include(r => r.Service)
                .Where(r => r.Service!.ProfessionalId == professionalId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ServiceRequest>> GetRequestsForClientAsync(Guid clientId)
        {
            return await _context.ServiceRequests
                .Include(r => r.Service)
                .Where(r => r.ClientId == clientId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}