using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaServicios.API.Models;
using SistemaServicios.API.Services;
using System.Security.Claims;

namespace SistemaServicios.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RequestsController : ControllerBase
    {
        private readonly RequestService _requestService;

        public RequestsController(RequestService requestService)
        {
            _requestService = requestService;
        }

        [HttpPost] 
        public async Task<ActionResult<ServiceRequest>> CreateRequest(int serviceId, string message)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Unauthorized();

                var clientId = Guid.Parse(userIdClaim.Value);
                var request = await _requestService.CreateRequestAsync(serviceId, clientId, message);
                
                return Ok(request);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("my-requests")] 
        public async Task<ActionResult<IEnumerable<ServiceRequest>>> GetMyRequests()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            var clientId = Guid.Parse(userIdClaim.Value);
            return Ok(await _requestService.GetRequestsForClientAsync(clientId));
        }

        [HttpGet("incoming")] 
        public async Task<ActionResult<IEnumerable<ServiceRequest>>> GetIncomingRequests()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            var professionalId = Guid.Parse(userIdClaim.Value);
            return Ok(await _requestService.GetRequestsForProfessionalAsync(professionalId));
        }
    }
}