using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ServicesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Service>>> GetServices(string? search, int? categoryId)
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

        [HttpGet("{id}")]
        public async Task<ActionResult<Service>> GetService(int id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
            {
                return NotFound();
            }

            return service;
        }

        [HttpPost]
        public async Task<ActionResult<Service>> PostService(Service service)
        {
            service.CreatedAt = DateTime.UtcNow;
            service.UpdatedAt = null;

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetService", new { id = service.Id }, service);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutService(int id, Service service)
        {
            if (id != service.Id)
            {
                return BadRequest("El ID de la URL no coincide con el cuerpo de la petición.");
            }

            service.UpdatedAt = DateTime.UtcNow;
            
            _context.Entry(service).State = EntityState.Modified;
            _context.Entry(service).Property(x => x.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ServiceExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ServiceExists(int id)
        {
            return _context.Services.Any(e => e.Id == id);
        }
    }
}