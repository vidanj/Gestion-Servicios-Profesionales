# Contrato: entorno de trabajo en Codespaces

## Servicios

| Servicio | Imagen | Puertos | Notas |
|---|---|---|---|
| `app` | Base de devcontainers + features .NET 9 y Node 24 | 3000 (frontend), 5000 (API) | Espacio de trabajo montado en `/workspaces/<repo>` |
| `db` | `postgres:18-alpine` | 5432 (solo interno) | Volumen con nombre; se pierde al borrar el codespace |

## Secretos recomendados (`devcontainer.json` → `secrets`)

`JWT_KEY`, `SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASSWORD`, `SMTP_FROM`.

## Ciclo de vida

| Gancho | Comandos | Falla si |
|---|---|---|
| `postCreateCommand` → `post-create.sh` | `dotnet tool restore`, `dotnet restore backend`, `npm ci` en `frontend/`, `npx playwright install --with-deps chromium`, generar `.env`, `dotnet ef database update` | Falta un secreto obligatorio (mensaje con su nombre) |
| `postStartCommand` | Esperar a `db` con `pg_isready` | — |

## Variables calculadas en el codespace

```bash
NEXT_PUBLIC_ALLOWED_PATH="https://${CODESPACE_NAME}-5000.${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN}"
ALLOWED_ORIGINS="https://${CODESPACE_NAME}-3000.${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN}"
```

## Comandos esperados dentro del codespace

```bash
dotnet run --project backend/SistemaServicios.API   # API → puerto 5000
cd frontend && npm run dev                           # UI  → puerto 3000
cd backend && dotnet test                            # unitarias + integración
cd frontend && npm test                              # Playwright E2E
dotnet tool run csharpier check backend/             # formato
```
