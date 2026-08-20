using FluentAssertions;
using Microsoft.AspNetCore.Http;
using SistemaServicios.API.Extensions;
using Xunit;

namespace SistemaServicios.Tests.Unit;

/// <summary>
/// Comprueba qué rutas se consideran sondeo de plataforma.
/// </summary>
/// <remarks>
/// Merece pruebas propias por un motivo concreto: silenciar la raíz con una comparación
/// por prefijo en lugar de exacta dejaría el log <b>completamente mudo</b> y desactivaría
/// la redirección entera, porque toda
/// ruta empieza por "/". El fallo sería silencioso y solo se notaría durante un incidente,
/// que es el peor momento posible para descubrir que no hay logs.
/// </remarks>
public class RutasDeSondeoTests
{
    private static DefaultHttpContext ContextoCon(string ruta)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = ruta;
        return context;
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public void NoRegistraLasRutasDeSondeo(string ruta)
    {
        _ = LoggingConfiguration.DebeRegistrarPeticion(ContextoCon(ruta)).Should().BeFalse();
    }

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/Users")]
    [InlineData("/api/Admin/backups")]
    [InlineData("/swagger")]
    // Comparte prefijo textual con "/health" pero no es un segmento suyo: debe registrarse.
    [InlineData("/healthcheck")]
    // Lo que de verdad importa: que silenciar la raíz no silencie todo lo que cuelga de ella.
    [InlineData("/api")]
    public void RegistraTodoLoDemas(string ruta)
    {
        _ = LoggingConfiguration.DebeRegistrarPeticion(ContextoCon(ruta)).Should().BeTrue();
    }
}
