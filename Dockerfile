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

# ── Stage 2: Migration bundle ────────────────────────────────────────────────
FROM build AS migrations
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet ef migrations bundle \
    --project backend/SistemaServicios.API/SistemaServicios.API.csproj \
    --self-contained -r linux-x64 \
    -o /app/efbundle

# ── Stage 3: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# pg_dump para el respaldo de base de datos (BackupService).
# La imagen aspnet:9.0 (Debian bookworm) no lo trae, y el paquete por defecto de
# bookworm es la version 15: pg_dump SE NIEGA a volcar un servidor de version
# mayor que la suya, asi que 15 fallaria contra PostgreSQL 16/17/18. Un cliente
# mas nuevo si puede volcar servidores mas viejos, por eso se fija el 18 desde
# el repositorio oficial PGDG y no el de bookworm.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl gnupg \
    && install -d /usr/share/postgresql-common/pgdg \
    && curl -fsSL https://www.postgresql.org/media/keys/ACCC4CF8.asc \
        -o /usr/share/postgresql-common/pgdg/apt.postgresql.org.asc \
    && echo "deb [signed-by=/usr/share/postgresql-common/pgdg/apt.postgresql.org.asc] https://apt.postgresql.org/pub/repos/apt bookworm-pgdg main" \
        > /etc/apt/sources.list.d/pgdg.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client-18 \
    && apt-get purge -y --auto-remove gnupg \
    && rm -rf /var/lib/apt/lists/*
# curl se conserva a proposito: lo necesita la instruccion HEALTHCHECK de mas abajo.
# La imagen base no trae ningun cliente HTTP.

# Destino de los respaldos. Fuera de /app para poder montarlo como volumen:
# sin un montaje explicito el .sql muere con el contenedor (ver issue #126).
ENV BACKUP_DIR=/var/backups/gsp
RUN mkdir -p /var/backups/gsp

COPY --from=build /app/publish .
COPY --from=migrations /app/efbundle .
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh efbundle

# Render expone el puerto 10000 por defecto en el plan free.
# ASPNETCORE_URLS sobreescribe el Kestrel configurado en appsettings.json.
# Sin seccion Kestrel en appsettings.json, ASPNETCORE_URLS controla el binding.
ENV ASPNETCORE_URLS=http://+:10000

# La imagen base aspnet:9.0 trae ASPNETCORE_HTTP_PORTS=8080. ASPNETCORE_URLS gana de
# todos modos, pero el host avisa en cada arranque de que esta ignorando ese valor.
# Vaciarlo elimina el aviso sin cambiar el puerto: el binding lo sigue decidiendo URLS.
ENV ASPNETCORE_HTTP_PORTS=
EXPOSE 10000

# Sonda de readiness: la instancia solo se considera sana si ademas alcanza la base.
# start-period cubre el tiempo que entrypoint.sh dedica a aplicar migraciones con
# efbundle antes de que Kestrel empiece a escuchar; sin ese margen, el contenedor se
# marcaria como enfermo durante un arranque perfectamente normal.
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD curl --fail --silent --show-error http://localhost:10000/health/ready || exit 1

ENTRYPOINT ["./entrypoint.sh"]
