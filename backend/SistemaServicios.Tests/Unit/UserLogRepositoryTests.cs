using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaServicios.API.Data;
using SistemaServicios.API.Models;
using SistemaServicios.API.Repositories;
using Xunit;

namespace SistemaServicios.Tests.Unit;

public class UserLogRepositoryTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static User CrearUsuario() => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Juan",
        LastName = "Pérez",
        Email = $"{Guid.NewGuid()}@test.com",
        PasswordHash = "hash",
        Role = UserRole.Client,
        Status = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    private static UserLog CrearLog(Guid userId, LogStatus status = LogStatus.Exitoso, LogAction action = LogAction.CreacionUsuario) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Action = action,
        Detail = "Test log.",
        Status = status,
        CreatedAt = DateTime.UtcNow,
    };

    // ─── GetLogsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLogsAsyncRetornaLogsOrdenadosPorFecha()
    {
        await using var ctx = CreateContext();
        var user = CrearUsuario();
        ctx.Users.Add(user);

        var log1 = CrearLog(user.Id);
        log1.CreatedAt = DateTime.UtcNow.AddHours(-2);
        var log2 = CrearLog(user.Id);
        log2.CreatedAt = DateTime.UtcNow;

        ctx.UserLogs.AddRange(log1, log2);
        await ctx.SaveChangesAsync();

        var repo = new UserLogRepository(ctx);
        var (logs, total) = await repo.GetLogsAsync(1, 10, null, null, null);

        total.Should().Be(2);
        logs.First().Id.Should().Be(log2.Id);
    }

    [Fact]
    public async Task GetLogsAsyncPaginacionRetornaTamanoCorrecto()
    {
        await using var ctx = CreateContext();
        var user = CrearUsuario();
        ctx.Users.Add(user);
        ctx.UserLogs.AddRange(Enumerable.Range(0, 15).Select(_ => CrearLog(user.Id)));
        await ctx.SaveChangesAsync();

        var repo = new UserLogRepository(ctx);
        var (logs, total) = await repo.GetLogsAsync(1, 5, null, null, null);

        total.Should().Be(15);
        logs.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetLogsAsyncFiltraPorStatus()
    {
        await using var ctx = CreateContext();
        var user = CrearUsuario();
        ctx.Users.Add(user);
        ctx.UserLogs.Add(CrearLog(user.Id, LogStatus.Exitoso));
        ctx.UserLogs.Add(CrearLog(user.Id, LogStatus.Error));
        ctx.UserLogs.Add(CrearLog(user.Id, LogStatus.Alerta));
        await ctx.SaveChangesAsync();

        var repo = new UserLogRepository(ctx);
        var (logs, total) = await repo.GetLogsAsync(1, 10, LogStatus.Exitoso, null, null);

        total.Should().Be(1);
        logs.First().Status.Should().Be(LogStatus.Exitoso);
    }

    [Fact]
    public async Task GetLogsAsyncFiltraPorUserId()
    {
        await using var ctx = CreateContext();
        var user1 = CrearUsuario();
        var user2 = CrearUsuario();
        ctx.Users.AddRange(user1, user2);
        ctx.UserLogs.Add(CrearLog(user1.Id));
        ctx.UserLogs.Add(CrearLog(user1.Id));
        ctx.UserLogs.Add(CrearLog(user2.Id));
        await ctx.SaveChangesAsync();

        var repo = new UserLogRepository(ctx);
        var (logs, total) = await repo.GetLogsAsync(1, 10, null, user1.Id, null);

        total.Should().Be(2);
        logs.Should().OnlyContain(l => l.UserId == user1.Id);
    }

    [Fact]
    public async Task GetLogsAsyncFiltraPorAction()
    {
        await using var ctx = CreateContext();
        var user = CrearUsuario();
        ctx.Users.Add(user);
        ctx.UserLogs.Add(CrearLog(user.Id, action: LogAction.CreacionUsuario));
        ctx.UserLogs.Add(CrearLog(user.Id, action: LogAction.EliminacionUsuario));
        ctx.UserLogs.Add(CrearLog(user.Id, action: LogAction.CambioContrasena));
        await ctx.SaveChangesAsync();

        var repo = new UserLogRepository(ctx);
        var (logs, total) = await repo.GetLogsAsync(1, 10, null, null, LogAction.EliminacionUsuario);

        total.Should().Be(1);
        logs.First().Action.Should().Be(LogAction.EliminacionUsuario);
    }

    [Fact]
    public async Task GetLogsAsyncSinRegistrosRetornaCero()
    {
        await using var ctx = CreateContext();
        var repo = new UserLogRepository(ctx);
        var (logs, total) = await repo.GetLogsAsync(1, 10, null, null, null);

        total.Should().Be(0);
        logs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLogsAsyncFiltraCombinado()
    {
        await using var ctx = CreateContext();
        var user = CrearUsuario();
        ctx.Users.Add(user);
        ctx.UserLogs.Add(CrearLog(user.Id, LogStatus.Exitoso, LogAction.CreacionUsuario));
        ctx.UserLogs.Add(CrearLog(user.Id, LogStatus.Error, LogAction.CreacionUsuario));
        ctx.UserLogs.Add(CrearLog(user.Id, LogStatus.Exitoso, LogAction.EliminacionUsuario));
        await ctx.SaveChangesAsync();

        var repo = new UserLogRepository(ctx);
        var (logs, total) = await repo.GetLogsAsync(1, 10, LogStatus.Exitoso, null, LogAction.CreacionUsuario);

        total.Should().Be(1);
        logs.First().Status.Should().Be(LogStatus.Exitoso);
        logs.First().Action.Should().Be(LogAction.CreacionUsuario);
    }

    // ─── AddLogAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AddLogAsyncGuardaLogYRetornaConId()
    {
        await using var ctx = CreateContext();
        var user = CrearUsuario();
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var repo = new UserLogRepository(ctx);
        var log = CrearLog(user.Id);
        var resultado = await repo.AddLogAsync(log);

        resultado.Id.Should().NotBe(Guid.Empty);
        ctx.UserLogs.Count().Should().Be(1);
    }

    [Fact]
    public async Task AddLogAsyncPersisteTodosLosCampos()
    {
        await using var ctx = CreateContext();
        var user = CrearUsuario();
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var repo = new UserLogRepository(ctx);
        var log = new UserLog
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Action = LogAction.CambioContrasena,
            Detail = "Detalle de prueba.",
            Status = LogStatus.Alerta,
            CreatedAt = DateTime.UtcNow,
        };

        var resultado = await repo.AddLogAsync(log);

        resultado.Action.Should().Be(LogAction.CambioContrasena);
        resultado.Detail.Should().Be("Detalle de prueba.");
        resultado.Status.Should().Be(LogStatus.Alerta);
        resultado.UserId.Should().Be(user.Id);
    }
}
