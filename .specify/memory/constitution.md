<!--
Sync Impact Report
- Cambio de versión: plantilla sin versión → 1.0.0 (ratificación de la línea base) → 1.1.0
- 1.0.0: los principios se derivan de las reglas ya vigentes (`.claude/RULES.md`, CI en
  `.github/workflows/`, `.editorconfig`, `.csharpierrc.json`). No se inventó ninguna regla nueva.
- 1.1.0 (MINOR): se amplía el Principio VII con una excepción acotada que permite `driver.js`
  exclusivamente para guías interactivas (tours). Origen: piloto `specs/002-guia-interactiva-tours`.
- Principios añadidos: I–VII. Secciones añadidas: Seguridad y Datos, Flujo de Trabajo y Calidad.
- Secciones eliminadas: ninguna.
- Plantillas revisadas: plan-template.md (la sección Constitution Check lee este archivo en tiempo
  de ejecución, no requiere cambios), spec-template.md y tasks-template.md (sin cambios).
- TODO diferidos: ninguno.
-->

# Constitución de Gestión de Servicios Profesionales

## Core Principles

### I. Especificación y plan aprobados antes que el código

Toda feature, corrección no trivial o cambio de infraestructura empieza por sus artefactos
SDD en `specs/<NNN-nombre>/` (`spec.md` → `plan.md` → `tasks.md`) y MUST contar con aprobación
explícita del responsable antes de `/speckit-implement`. Ningún agente ni persona crea, edita o
reemplaza código fuente sin esa aprobación. El `spec.md` describe el **qué** y el **por qué**;
el **cómo** vive en `plan.md`.

*Razón:* el proyecto ya exigía "planear antes de implementar"; SDD convierte ese plan en un
artefacto versionado y trazable en lugar de una conversación que se pierde.

### II. Capas del backend inviolables

El flujo de una petición MUST ser `Controller → Service (Interfaces/) → Repository →
AppDbContext`. Un controller solo mapea HTTP (validación de entrada y códigos de estado) y
nunca toca `AppDbContext`. La lógica de negocio vive en `Services/`; el acceso a datos, en
`Repositories/`. Toda dependencia nueva se registra en
`Extensions/ApplicationServiceExtensions.cs`, nunca en `Program.cs`.

*Razón:* permite probar la lógica en aislamiento y mantener un único punto de composición.

### III. Contratos por DTO y autorización explícita

Las entidades de `Models/` MUST NOT salir ni entrar por la API: todo pasa por DTOs de
`DTOs/<Módulo>/`. Todo endpoint declara su acceso con `[Authorize(Roles = …)]` respetando la
jerarquía Admin > Professional > Client; solo es público lo que el responsable apruebe como
público. Contraseñas con BCrypt y tokens con `TokenService`, sin mecanismos alternativos.
Nunca se registran ni se devuelven hashes, tokens JWT ni PINs.

### IV. El esquema cambia solo por migración EF

Todo cambio de esquema MUST hacerse con `dotnet ef migrations add`. No se editan migraciones
ya subidas ni se modifica la base a mano; `Migrations/` se versiona. El borrado de usuarios es
lógico (soft delete), nunca físico. Las operaciones destructivas (`scripts/restore.*`,
`dotnet ef database drop`, `DELETE`/`TRUNCATE` masivos) solo se ejecutan a petición explícita y
con confirmación previa.

### V. Pruebas obligatorias y honestas (NON-NEGOTIABLE)

- Lógica nueva MUST entregarse con pruebas unitarias en `SistemaServicios.Tests/Unit/`.
- Endpoint nuevo o modificado MUST entregarse con prueba de integración en `Integration/`
  usando `CustomWebApplicationFactory` (InMemory DB + JWT de prueba).
- Los E2E del frontend usan **Playwright** (`frontend/tests/*.spec.ts`) y corren contra
  respuestas simuladas (MSW o `page.route`), nunca contra el backend ni la base reales. No se
  instala otra herramienta de automatización (Puppeteer, Selenium, Cypress u otras).
- Ninguna prueba se borra, se salta (`.skip`) ni se reescribe para que la suite pase; un fallo
  se reporta con su salida.

### VI. Calidad automática sin silenciadores

El backend MUST pasar CSharpier (`printWidth` 100, 4 espacios, LF), StyleCop (SA) y Roslynator
(RCS); el frontend, ESLint y TypeScript estricto. La advertencia se arregla, no se silencia:
prohibidos `#pragma warning disable`, entradas nuevas en `.editorconfig` para tapar un aviso,
`any` nuevos y `@ts-ignore`. El CI (`backend-lint.yml`, `backend-tests.yml`,
`frontend-tests.yml`, `docker-image.yml`, `codeql.yml`) MUST estar en verde antes de pedir
revisión.

### VII. Stack del frontend cerrado

Rutas en `app/` (App Router); lo reutilizable en `src/`. Las features nuevas MUST seguir el
corte de `src/features/solicitudes/` (`domain / application / infrastructure / presentation`).
HTTP solo con Axios desde `src/services/`, con base URL tomada de `NEXT_PUBLIC_ALLOWED_PATH`.
Estado global con Zustand; formularios con React Hook Form + Yup; UI con **Chakra UI v3 +
Tailwind v4**. No se agrega otra librería de estado, formularios ni UI.

**Excepción acotada (v1.1.0):** se permite `driver.js` **exclusivamente** para guías
interactivas (tours de producto). Condiciones: se encapsula en una sola feature
(`src/features/tours/`), se carga solo en cliente, se estiliza con los tokens del tema de
Chakra y no se usa para ningún otro componente de UI. Cualquier uso fuera de ese alcance
requiere una nueva enmienda.

## Seguridad y Datos

- La configuración vive en `.env` (raíz del repo, cargado con DotNetEnv) y **nunca** se
  commitea. Toda variable nueva se agrega también a `.env.example` con un valor de ejemplo,
  nunca el real.
- Nada de credenciales en código, pruebas, documentación ni mensajes de commit.
- Nunca se commitean `*.sql`, `backups/`, `coverage/`, `test-results/`, `playwright-report/`
  ni los avatares de `wwwroot/uploads/avatars/`.
- Los secretos de CI/CD e infraestructura (tokens de nube, deploy hooks, estado remoto de
  Terraform) viven en GitHub Secrets / Environments o en el gestor de secretos del proveedor,
  nunca en el repositorio.

## Flujo de Trabajo y Calidad

- **Ramas:** nunca se trabaja directo sobre `main` ni `dev`. Prefijos `feat/`, `fix/`,
  `test/`, `chore/`, `ci/`, `docs/`, `perf/`, `refactor/`, normalmente con número de issue.
- **Commits:** convencionales con ámbito (`feat(backend): …`, `docs: …`). Ni commit, ni push,
  ni merge, ni PR sin confirmación explícita del responsable.
- **Integración:** rama de trabajo → PR a `dev` (plantilla de `.github/PULL_REQUEST_TEMPLATE/`,
  con `Resuelve #N` y enlace al `specs/<NNN>/` correspondiente) → `dev` → `main`.
- **Trazabilidad SDD:** cada tarea de `tasks.md` se liga a una historia (`[USn]`) y a los
  requisitos (`FR-###`) que cubre; cada PR enlaza el spec que implementa. `/speckit-analyze`
  se ejecuta y queda sin hallazgos CRITICAL antes de implementar.
- **Validación visual:** la validación de UI/UX es manual del responsable; al terminar un
  cambio de interfaz se indica qué abrir, qué hacer y qué debería verse.
- **Documentación:** un cambio que altere endpoints, variables de entorno o pasos de arranque
  actualiza el `README.md` en el mismo PR.

## Governance

- Esta constitución es la fuente versionada de las reglas del proyecto y prevalece sobre
  cualquier otra práctica. `CLAUDE.md` y `.claude/RULES.md` son guías operativas locales para
  agentes y no pueden contradecirla; ante un conflicto, se corrige la guía.
- **Enmiendas:** solo por PR que modifique este archivo, con el Sync Impact Report, la
  justificación y la aprobación del responsable. Una enmienda no se hace dentro de
  `/speckit-analyze` ni de forma implícita en un plan.
- **Versionado semántico:** MAJOR = eliminación o redefinición incompatible de un principio;
  MINOR = principio o sección nueva, o ampliación material (por ejemplo, una excepción);
  PATCH = aclaraciones y redacción.
- **Cumplimiento:** la sección *Constitution Check* de cada `plan.md` evalúa estos principios;
  toda violación se justifica en *Complexity Tracking* o bloquea el plan. Los revisores de PR
  verifican el cumplimiento.

**Version**: 1.1.0 | **Ratified**: 2026-09-11 | **Last Amended**: 2026-09-11
