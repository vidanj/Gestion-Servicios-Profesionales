using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Services
{
    public class ProfessionalService
    {
        private readonly AppDbContext _context;

        public ProfessionalService(AppDbContext context)
        {
            _context = context;
        }

        // Lógica para obtener servicios con filtros
        public async Task<IEnumerable<Service>> GetAllServicesAsync(string? search, int? categoryId)
        {
            var query = _context.Services.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.Title.Contains(search) || (s.Description != null && s.Description.Contains(search)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(s => s.CategoryId == categoryId);
            }

            return await query.ToListAsync();
        }

        // Lógica para obtener un solo servicio
        public async Task<Service?> GetServiceByIdAsync(int id)
        {
            return await _context.Services.FindAsync(id);
        }

        // Lógica para crear un servicio
        public async Task<Service> CreateServiceAsync(Service service)
        {
            service.CreatedAt = DateTime.UtcNow;
            service.UpdatedAt = null;
            service.IsActive = true;

            _context.Services.Add(service);
            await _context.SaveChangesAsync();
            return service;
        }
    }
}