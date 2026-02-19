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

        // 1. LISTAR USUARIOS (GET: api/Users)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // 2. OBTENER USUARIO POR ID (GET: api/Users/{guid})
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // 3. CREAR USUARIO (POST: api/Users)
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            
            
            user.CreatedAt = DateTime.UtcNow; // Forzamos la fecha de creación
            user.UpdatedAt = DateTime.UtcNow;
            
            

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.Id }, user);
        }

        // 4. EDITAR USUARIO (PUT: api/Users/{guid})
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(Guid id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest("El ID de la URL no coincide con el del cuerpo de la petición.");
            }

            user.UpdatedAt = DateTime.UtcNow;

            _context.Entry(user).State = EntityState.Modified;


            _context.Entry(user).Property(x => x.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
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

        // 5. ELIMINAR USUARIO (DELETE: api/Users/{guid})
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(Guid id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}