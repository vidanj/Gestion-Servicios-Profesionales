using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Category>> GetAllActiveAsync()
    {
        return await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        return await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> ExistsAsync(int id, bool mustBeActive = false)
    {
        return mustBeActive
            ? await _context.Categories.AnyAsync(c => c.Id == id && c.IsActive)
            : await _context.Categories.AnyAsync(c => c.Id == id);
    }
}
