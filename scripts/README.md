# Guiones del proyecto

Cada uno existe en dos variantes equivalentes: `.ps1` para PowerShell y `.sh` para shell.
Ninguno contiene credenciales; todas las variables se leen del entorno o del `.env`. Todos
terminan con código distinto de cero cuando fallan, para que puedan encadenarse.

## Respaldo de la base de datos

| Guion | Qué hace |
|---|---|
| `backup.ps1` · `backup.sh` | Genera un respaldo con `pg_dump` en `BACKUP_DIR` |
| `restore.ps1` · `restore.sh` | **Destructivo.** Restaura un respaldo, sobrescribiendo la base. Solo a petición explícita y con confirmación (constitución, Principio IV) |

## Entorno de monitoreo

| Guion | Qué hace |
|---|---|
| `monitoreo-up.ps1` · `.sh` | Levanta el entorno, espera a que las cinco herramientas respondan sanas e imprime sus direcciones. `-SinApi` / `--sin-api` para levantar solo el monitoreo |
| `monitoreo-down.ps1` · `.sh` | Lo derriba conservando los datos. `-Purgar` / `--purgar` elimina también los volúmenes, con confirmación explícita |
| `generar-trafico.ps1` · `.sh` | Tráfico sintético variado para poblar los tableros antes de una captura |
| `verificar-monitoreo.ps1` · `.sh` | Recorre el catálogo de métricas, comprueba las 10 alarmas cargadas y el estado de Grafana |

**`monitoreo-up` se detiene si falta `GRAFANA_ADMIN_PASSWORD`**, en lugar de arrancar con la
contraseña por defecto en silencio. Esa instalación acabaría en las capturas del documento.

**`verificar-monitoreo.sh` es el mismo guion que ejecuta la integración continua.** Lo que se
comprueba en un equipo de trabajo es exactamente lo que se comprueba en el pipeline; no hay dos
definiciones de «el monitoreo está bien».

## Despliegue

| Guion | Qué hace |
|---|---|
| `desplegar.ps1` · `.sh` | Dispara el despliegue, espera, sondea hasta 600 s exigiendo tres respuestas sanas consecutivas y ejecuta la verificación de humo. `-Revertir <sha>` / `--revertir <sha>` para volver a una versión anterior |

Equivale exactamente a [`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml), y existe
para poder ejecutar y depurar el despliegue sin pasar por la interfaz de Actions.

**Revertir no es otra operación: es desplegar el commit sano anterior.** Así ese camino se
ejercita en cada despliegue, en lugar de ser una ruta que nadie ha recorrido hasta el día del
incidente.

## Variables de entorno

| Variable | La usan | Sensible |
|---|---|---|
| `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` | respaldo y restauración | `DB_PASSWORD` sí |
| `BACKUP_DIR` | respaldo y restauración | No |
| `GRAFANA_ADMIN_PASSWORD` | `monitoreo-up` | **Sí** |
| `RENDER_DEPLOY_HOOK_URL` | `desplegar` | **Sí** |
| `RENDER_SERVICE_URL` | `desplegar` | No |

Las sensibles viven en el `.env` local (que nunca se versiona) o en los secretos de la plataforma
de integración continua. Ninguna se escribe en estos archivos.

## Recorrido típico

```powershell
.\scripts\monitoreo-up.ps1          # levanta el entorno
.\scripts\generar-trafico.ps1       # puebla los tableros
.\scripts\verificar-monitoreo.ps1   # comprueba que no falta nada
# ... abrir http://localhost:3001 ...
.\scripts\monitoreo-down.ps1        # apaga, conservando el histórico
```

El recorrido completo de validación, incluida la prueba de alarma de extremo a extremo, está en
[`specs/008-monitoreo-metricas-alertas/quickstart.md`](../specs/008-monitoreo-metricas-alertas/quickstart.md).
