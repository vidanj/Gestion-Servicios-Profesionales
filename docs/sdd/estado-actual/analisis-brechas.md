# Análisis de brechas: qué falta para un SDD formal

> Línea base: `dev` @ 0159a9b (2026-09-11). Evidencia en [ingeniería inversa](ingenieria-inversa.md),
> [modelo de datos](er-diagram.md) y [trazabilidad](trazabilidad.md).

## 1. Madurez SDD: antes y después de esta instalación

Escala: **0** inexistente · **1** informal · **2** parcial · **3** formal y versionado.

| Dimensión | Antes | Después | Qué la sube a 3 |
|---|:--:|:--:|---|
| Gobernanza (reglas del proyecto) | 1 — `RULES.md` local, ignorado por git | **3** — constitución v1.1.0 versionada | — |
| Herramientas SDD | 0 | **3** — Spec Kit 1.0.6 y skills versionadas | — |
| Requisitos | 1 — issues, borradores ignorados, reporte en Word | 2 — 3 specs piloto | Specs de los módulos de negocio (fase 3) |
| Diseño y contratos | 1 — Swagger solo en Development; README incompleto | 2 — `contracts/` en los pilotos | Contrato OpenAPI exportado y versionado |
| Trazabilidad | 1 — pocos commits citan issue | 2 — matriz de línea base; tareas ligadas a FR | Convención de commit con tarea y check de PR |
| Pruebas ligadas a requisitos | 1 — 4 casos formales para 40 endpoints | 2 — escenario por FR en los pilotos | Specs de negocio con pruebas por FR |
| Verificación de consistencia | 0 | 2 — `analyze` aplicado a los pilotos | `converge` antes de cada PR |
| Automatización del proceso | 0 | 0 | Check de CI, plantilla de PR y hooks (fase 4) |

## 2. Brechas del proceso SDD

| ID | Brecha | Qué falta | Prioridad | Cómo se cierra |
|---|---|---|---|---|
| P1 | No hay specs de los módulos existentes | Especificar el comportamiento vigente a medida que se modifique cada módulo, sin retro-documentar todo | Alta | Fase 3: specs 004–006 |
| P2 | Requisitos fuera del repo (`.github/DRAFTS/` ignorado, `Reporte_Auditoria.docx`) | Mover los pendientes a specs o a issues enlazados | Alta | Al abrir cada spec, citar su origen |
| P3 | La plantilla de PR no pide el spec | Sección "Especificación" (enlace, tareas cerradas, resumen de `analyze`) | Alta | Fase 4 |
| P4 | Ningún control automático exige spec | Check de CI para PR `feat/` hacia `dev` que busque un enlace a `specs/` | Media | Fase 4 |
| P5 | Commits sin referencia a issue o tarea (`cambios momentaneos`, `vefgeg`) | Convención: `tipo(ámbito): mensaje (T012, #215)` | Media | Guía de implementación §5 |
| P6 | Contrato de la API no versionado | Exportar OpenAPI en CI a `docs/api/openapi.json` y detectar diferencias | Media | Spec dedicado |
| P7 | Plantillas de Spec Kit en inglés | Overrides en español en `.specify/templates/overrides/` | Baja | Fase 4 |
| P8 | Sin métricas del proceso | Indicadores de la [guía §9](../sdd-implementation.md#9-seguimiento) | Media | Desde el primer piloto implementado |
| P9 | Definition of Ready/Done sin SDD | DoR: spec aprobado y `analyze` sin CRITICAL. DoD: `converge` sin brechas, CI en verde, `quickstart` validado | Alta | Constitución (flujo de trabajo) |
| P10 | README desactualizado (stack Next 14/React 18; tabla de endpoints con 3 de 10 módulos; accesos distintos del código) | Actualizarlo en el mismo PR que toque endpoints (regla vigente) | Media | Issue DOCS |

## 3. Brechas del producto detectadas por la ingeniería inversa

| ID | Brecha | Evidencia | Severidad | Se atiende en |
|---|---|---|---|---|
| B1 | **Cotización sin implementar** | `Quote` solo existe como entidad; sin controller, servicio, repositorio, UI ni pruebas. Hay ramas remotas sin integrar (`feat/110-cotizacion-calculo-dinamico`, `feat/cotizador-precio-estimado`) | Alta | Spec 004 (propuesto) |
| B2 | **Verificación de profesionales sin implementar** | `Verification` solo existe como entidad; `VerificationStatus` nunca se usa | Alta | Spec 005 (propuesto) |
| B3 | **Reglas de calificación incompletas** | `RatingService.CreateRatingAsync` no valida que la solicitud esté Completada ni que pertenezca al cliente; toma `ProfessionalId` del cuerpo; Professional y Admin también pueden calificar; `User.AverageRating` nunca se actualiza | Alta | Spec 006 (propuesto) |
| B4 | **Bitácora falsificable** | `POST /api/UserLogs` solo exige autenticación y `CreateUserLogDto` recibe `UserId` en el cuerpo: cualquier usuario autenticado puede registrar acciones a nombre de otro. El README describe esa tabla como "evidencia" | Alta (seguridad) | Issue BUG/SEC |
| B5 | Borrado en cascada Categories → Services → Requests → Ratings | Snapshot de migraciones (M1 del [modelo](er-diagram.md#7-observaciones-del-modelo)) | Media (latente) | Issue BUG |
| B6 | `next.config.ts` pasa **todo** el `.env` a `env` de Next | Cualquier referencia a `process.env.X` en código de cliente incrusta ese valor (incluidos `DB_PASSWORD`, `JWT_KEY`, SMTP) en el bundle público | Media-alta (latente) | Issue SEC: exponer solo `NEXT_PUBLIC_*` |
| B7 | `.env.example` trae un `JWT_KEY` con forma de clave real | Línea 7 de `.env.example` | Media | Reemplazar por un marcador y rotar la clave si se usó |
| B8 | El frontend no sigue su propia arquitectura | 9 de 17 páginas llaman a `fetch` directamente, sin `src/services/`; `register` usa `http://localhost:5000` como respaldo; los handlers de MSW fijan la URL | Media | Refactor por feature |
| B9 | Módulos sin pruebas | Servicios, Categorías, Solicitudes y Calificaciones sin pruebas unitarias ni de integración; Usuarios sin integración; catálogo, detalle, profesionista y solicitudes sin E2E | Alta | **Piloto 001** (E2E) y specs de negocio |
| B10 | Mocks de E2E distintos de lo que dicen las reglas | `MSWProvider` está comentado en `app/layout.tsx`; las pruebas usan `page.route` | Baja | Constitución v1.1.0 admite ambos |
| B11 | Accesos más amplios que lo documentado | `GET /api/Users/{id}` y `GET /api/Users/stats/registrations` solo exigen autenticación | Media | Revisar en spec de usuarios |
| B12 | **Despliegue manual y migración acoplada al arranque** | Render se dispara a mano (#189); `entrypoint.sh` con `set -e` tumba el contenedor si falla `efbundle` | Alta | **Piloto 003** |
| B13 | Sin entorno reproducible ni IaC | No hay `.devcontainer/`, `*.tf` ni `render.yaml` | Media | **Piloto 003** |
| B14 | Sin onboarding; navegación igual para todos los roles | `nav.tsx` muestra Administración, Mis Servicios y Solicitudes a cualquiera | Media | **Piloto 002** (+ spec de navegación por rol) |
| B15 | Metadatos por defecto | `title: "Create Next App"`, `lang="en"` en `app/layout.tsx` | Baja | Issue CHORE |
| B16 | Regla de negocio dudosa | `POST /api/ServiceRequests` permite al rol Professional crear solicitudes | Baja | Confirmar en spec de solicitudes |
| B17 | Rol con dos representaciones en el frontend | `auth-store.ts` guarda `"Client"` desde el login y `"1"` desde el perfil (`String(data.role)`) | Media | Normalizado en el piloto 002 (R7); corregir en el store |
| B18 | Recuperación de contraseña desconectada | `/recovery` solo importa `Link`; nunca llama a `POST /api/Auth/forgot-password`, que existe | Media | Issue BUG |
| B19 | Detalle de excepción en respuestas 500 | `error = ex.Message` en 15 lugares de 4 controllers (Services 7, ServiceRequests 5, Categories 2, Ratings 1), pese a la sanitización de #140 | Media (seguridad) | Issue SEC |

## 4. Hoja de ruta para cerrar las brechas

| Orden | Acción | Brechas | Tipo |
|---|---|---|---|
| 1 | Corregir la bitácora falsificable (tomar `UserId` del token) y exponer solo `NEXT_PUBLIC_*` | B4, B6, B7 | `fix/` con prueba de integración |
| 2 | Implementar los pilotos a), b) y c) desde sus `tasks.md` | B9, B12, B13, B14 | `/speckit-implement` con aprobación |
| 3 | Specs 004 Cotización, 005 Verificación y 006 Reglas de calificación | B1, B2, B3, P1 | `/speckit-specify` |
| 4 | Plantilla de PR, check de CI, overrides en español, skills propias | P3, P4, P7 | Fase 4 |
| 5 | Refactor del frontend a `src/services` por feature | B8 | Spec por feature |
| 6 | Revisar accesos y cascadas; actualizar el README | B5, B11, P10 | `fix/` y `docs/` |
