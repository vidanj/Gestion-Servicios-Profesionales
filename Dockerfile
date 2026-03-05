# ── Stage 1: Build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar archivos de proyecto primero para aprovechar cache de capas
COPY backend/SistemaServicios.sln                                      ./backend/
COPY backend/SistemaServicios.API/SistemaServicios.API.csproj          ./backend/SistemaServicios.API/
COPY backend/SistemaServicios.Tests/SistemaServicios.Tests.csproj      ./backend/SistemaServicios.Tests/

RUN dotnet restore backend/SistemaServicios.API/SistemaServicios.API.csproj

# Copiar el resto del codigo fuente
COPY backend/ ./backend/

RUN dotnet publish backend/SistemaServicios.API/SistemaServicios.API.csproj -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Render expone el puerto 10000 por defecto en el plan free.
# ASPNETCORE_URLS sobreescribe el Kestrel configurado en appsettings.json.
# ASPNETCORE_URLS aplica cuando NO hay configuracion Kestrel en appsettings.
# ASPNETCORE_Kestrel__ sobreescribe la seccion Kestrel de appsettings.json,
# forzando que el servidor escuche en todas las interfaces (0.0.0.0).
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_Kestrel__Endpoints__Http__Url=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "SistemaServicios.API.dll"]
