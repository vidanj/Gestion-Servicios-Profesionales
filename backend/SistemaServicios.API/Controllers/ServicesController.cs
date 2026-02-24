using Microsoft.AspNetCore.Mvc;
using SistemaServicios.API.Models;
using SistemaServicios.API.Services;

namespace SistemaServicios.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly ProfessionalService _professionalService;

        public ServicesController(ProfessionalService professionalService)
        {
            _professionalService = professionalService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Service>>> GetServices(string? search, int? categoryId)
        {
            var services = await _professionalService.GetAllServicesAsync(search, categoryId);
            return Ok(services);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Service>> GetService(int id)
        {
            var service = await _professionalService.GetServiceByIdAsync(id);
            if (service == null) return NotFound();
            return Ok(service);
        }

        [HttpPost]
        public async Task<ActionResult<Service>> PostService(Service service)
        {
            var createdService = await _professionalService.CreateServiceAsync(service);
            return CreatedAtAction("GetService", new { id = createdService.Id }, createdService);
        }
    }
}