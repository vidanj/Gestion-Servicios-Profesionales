using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.DTOs;
using SistemaServicios.API.Interfaces;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Repositories;

public class UserRepository : IUserRepository
{
    private static readonly Expression<Func<User, UserDto>> ToDto = u => new UserDto
    {
        Id = u.Id,
        Email = u.Email,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Role = u.Role,
        PhoneNumber = u.PhoneNumber,
        AverageRating = u.AverageRating,
        Status = u.Status,
        ProfileImageUrl = u.ProfileImageUrl,
        CreatedAt = u.CreatedAt,
    };

    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<User> users, int totalCount)> GetUsersAsync(
        int pageNumber,
        int pageSize
    )
    {
        var query = _context.Users.Where(u => u.Status == true);
        int totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (users, totalCount);
    }

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Status == true);

    public async Task<User?> GetByResetTokenAsync(string token) =>
        await _context.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == token && u.Status == true
        );

    public async Task<User?> GetUserByEmailAsync(string email) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<bool> EmailExistsAsync(string email) =>
        await _context.Users.AnyAsync(u => u.Email == email);

    public async Task<User> AddUserAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<UserRegistrationStatDto>> GetRegistrationsByDateAsync(int days)
    {
        var desde = DateTime.UtcNow.Date.AddDays(-days + 1);

        var resultados = await _context
            .Users.Where(u => u.CreatedAt >= desde)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        return Enumerable
            .Range(0, days)
            .Select(i => desde.AddDays(i))
            .Select(fecha => new UserRegistrationStatDto
            {
                Date = DateOnly.FromDateTime(fecha),
                Count = resultados.FirstOrDefault(r => r.Date == fecha)?.Count ?? 0,
            });
    }

    public async Task<(IEnumerable<UserDto> users, int totalCount)> GetUserDtosAsync(
        int pageNumber,
        int pageSize
    )
    {
        var query = _context.Users.Where(u => u.Status == true);
        int totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync();
        return (users, totalCount);
    }

    public async Task<UserDto?> GetUserDtoByIdAsync(Guid id) =>
        await _context
            .Users.Where(u => u.Id == id && u.Status == true)
            .Select(ToDto)
            .FirstOrDefaultAsync();
}
