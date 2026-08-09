using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Models;

namespace SistemaServicios.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<Service> Services { get; set; }

    public DbSet<Request> Requests { get; set; }

    public DbSet<Rating> Ratings { get; set; }

    public DbSet<Quote> Quotes { get; set; }

    public DbSet<Verification> Verifications { get; set; }

    public DbSet<UserLog> UserLogs { get; set; }

    // Configuración especial de relaciones (Fluent API)
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Regla: Un Servicio debe tener un precio base con 2 decimales
        modelBuilder.Entity<Service>().Property(s => s.BasePrice).HasPrecision(18, 2);

        // Regla: Una Solicitud tiene 2 Usuarios (Cliente y Profesional)
        // Evitamos que al borrar un usuario se rompa todo en cadena (Restrict)
        modelBuilder
            .Entity<Request>()
            .HasOne(r => r.Client)
            .WithMany()
            .HasForeignKey(r => r.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<Request>()
            .HasOne(r => r.Professional)
            .WithMany()
            .HasForeignKey(r => r.ProfessionalId)
            .OnDelete(DeleteBehavior.Restrict);

        // Lo mismo para las Calificaciones (Ratings)
        modelBuilder
            .Entity<Rating>()
            .HasOne(r => r.Client)
            .WithMany()
            .HasForeignKey(r => r.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<Rating>()
            .HasOne(r => r.Professional)
            .WithMany()
            .HasForeignKey(r => r.ProfessionalId)
            .OnDelete(DeleteBehavior.Restrict);

        // UserLog: al borrar usuario no elimina sus logs en cascada
        modelBuilder
            .Entity<UserLog>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Único: evita condición de carrera en el registro (antes solo se validaba en la app con EmailExistsAsync)
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        // Acelera listados/reportes ordenados u filtrados por fecha de alta
        modelBuilder.Entity<User>().HasIndex(u => u.CreatedAt);

        // Índice parcial: Status es de baja cardinalidad (bool), solo indexamos los activos
        // que es el filtro que se usa en la mayoría de las consultas (usuarios activos)
        modelBuilder.Entity<User>().HasIndex(u => u.Status).HasFilter("\"Status\" = true");
    }
}
