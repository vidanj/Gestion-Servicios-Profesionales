using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // 1. LISTAR USUARIOS (GET)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new {
                    u.Id, u.Email, u.FirstName, u.LastName, u.Role, u.PhoneNumber, u.Status, u.CreatedAt
                }).ToListAsync();

            return Ok(users);
        }

        // 2. OBTENER UN USUARIO (GET por ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUser(Guid id)
        {
            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new {
                    u.Id, u.Email, u.FirstName, u.LastName, u.Role, u.PhoneNumber, u.Status, u.CreatedAt
                }).FirstOrDefaultAsync();

            if (user == null) return NotFound();

            return Ok(user);
        }

        // 3. CREAR USUARIO (POST)
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User newUser)
        {
            newUser.Id = Guid.NewGuid();
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.UpdatedAt = DateTime.UtcNow;
            
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = newUser.Id }, newUser);
        }

        // 4. EDITAR USUARIO (PUT)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, User userUpdate)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.FirstName = userUpdate.FirstName;
            user.LastName = userUpdate.LastName;
            user.PhoneNumber = userUpdate.PhoneNumber;
            user.Role = userUpdate.Role;
            user.Status = userUpdate.Status;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 5. ELIMINAR USUARIO (DELETE)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}