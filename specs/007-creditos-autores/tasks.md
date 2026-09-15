# Tareas — Spec 007, módulo de créditos de autores

> Formato: `T### [P] [USn] descripción — ruta`. La marca `[P]` indica que la tarea puede ejecutarse
> en paralelo con otras marcadas igual dentro de la misma fase. `[USn]` liga la tarea a su historia.
>
> **Ninguna tarea se ejecuta sin aprobación explícita** (constitución, Principio I). Este
> documento es planeación.

---

## Fase 0 — Preparación

- [ ] T001 [P] Definir la sección de configuración con título y mensaje, con valores por defecto — `backend/SistemaServicios.API/appsettings.json`
- [ ] T002 [P] Crear la clase de opciones que enlaza esa sección — `backend/SistemaServicios.API/Configuration/CreditsOptions.cs`
- [ ] T003 Crear la entidad con sus anotaciones de longitud — `backend/SistemaServicios.API/Models/Credit.cs`
- [ ] T004 Registrar el conjunto de entidades, el índice compuesto de activo y orden, y la regla de restricción hacia el archivo — `backend/SistemaServicios.API/Data/AppDbContext.cs`
- [ ] T005 Generar la migración con la herramienta del ORM, sin editarla a mano — `backend/SistemaServicios.API/Migrations/`
- [ ] T006 Verificar que la migración se aplica y revierte en una base limpia

## Fase 1 — US1, consulta pública (P1, mínimo viable)

- [ ] T007 [P] [US1] Objeto de transferencia de salida de un autor — `DTOs/Credits/CreditDto.cs`
- [ ] T008 [P] [US1] Objeto de transferencia de la respuesta completa — `DTOs/Credits/CreditsPageDto.cs`
- [ ] T009 [US1] Interfaz del repositorio — `Interfaces/ICreditRepository.cs`
- [ ] T010 [US1] Repositorio con la consulta de activos ordenada, con desempate por identificador (FR-003) — `Repositories/CreditRepository.cs`
- [ ] T011 [US1] Interfaz del servicio — `Interfaces/ICreditService.cs`
- [ ] T012 [US1] Servicio que compone título y mensaje de configuración con la lista de autores (FR-001, FR-005, FR-007) — `Services/CreditService.cs`
- [ ] T013 [US1] Construcción de la ruta pública de la fotografía a partir de la referencia del archivo (FR-018) — `Services/CreditService.cs`
- [ ] T014 [US1] Controlador con el recurso de lectura sin autenticación (FR-006) — `Controllers/CreditsController.cs`
- [ ] T015 [US1] Registrar servicio y repositorio en la composición de dependencias — `Extensions/ApplicationServiceExtensions.cs`
- [ ] T016 [P] [US1] Pruebas unitarias del servicio: orden determinista, autor sin foto, lista vacía, exclusión de inactivos — `Tests/Unit/CreditServiceTests.cs`
- [ ] T017 [P] [US1] Prueba de integración: un visitante sin sesión obtiene la respuesta completa (SC-003) — `Tests/Integration/CreditsControllerTests.cs`

## Fase 2 — US2, administración (P2)

- [ ] T018 [P] [US2] Objetos de transferencia de alta y actualización, sin campo de fotografía — `DTOs/Credits/CreateCreditDto.cs`, `DTOs/Credits/UpdateCreditDto.cs`
- [ ] T019 [US2] Métodos de repositorio para alta, actualización, obtención por identificador y cálculo del orden final — `Repositories/CreditRepository.cs`
- [ ] T020 [US2] Validaciones del servicio: longitudes, orden no negativo y esquema del enlace (FR-012) — `Services/CreditService.cs`
- [ ] T021 [US2] Asignación de orden al final en el alta y estado activo por omisión (FR-014) — `Services/CreditService.cs`
- [ ] T022 [US2] Recursos de alta, actualización y consulta individual con política de rol administrador (FR-008, FR-009, FR-011) — `Controllers/CreditsController.cs`
- [ ] T023 [US2] Recurso de cambio de orden (FR-013) — `Controllers/CreditsController.cs`
- [ ] T024 [US2] Baja lógica que conserva registro y fotografía, idempotente (FR-010) — `Services/CreditService.cs`, `Controllers/CreditsController.cs`
- [ ] T025 [US2] Respuestas de error sin detalle de excepción en todo el controlador (FR-020) — `Controllers/CreditsController.cs`
- [ ] T026 [P] [US2] Pruebas unitarias de validación y de asignación de orden — `Tests/Unit/CreditServiceTests.cs`
- [ ] T027 [P] [US2] Pruebas de integración: acceso denegado para roles no administradores en cada recurso de escritura (SC-004) — `Tests/Integration/CreditsControllerTests.cs`
- [ ] T028 [P] [US2] Prueba de integración: un autor dado de baja desaparece de la consulta pública — `Tests/Integration/CreditsControllerTests.cs`

## Fase 3 — US3, fotografía (P3)

- [ ] T029 [US3] Recurso de carga de fotografía con política de rol administrador (FR-015) — `Controllers/CreditsController.cs`
- [ ] T030 [US3] Reutilizar la validación por contenido binario existente (FR-016) — `Services/CreditService.cs`
- [ ] T031 [US3] Límite de tamaño del archivo (FR-017) — `Services/CreditService.cs`
- [ ] T032 [US3] Persistir mediante la abstracción de almacenamiento, con el administrador como propietario — `Services/CreditService.cs`
- [ ] T033 [US3] Reemplazo: dejar vigente la fotografía más reciente (FR-019) — `Services/CreditService.cs`
- [ ] T034 [P] [US3] Pruebas unitarias: archivo falsificado rechazado y no almacenado; archivo excedido rechazado — `Tests/Unit/CreditServiceTests.cs`
- [ ] T035 [P] [US3] Prueba de integración del ciclo completo de carga y recuperación por el recurso público de archivos — `Tests/Integration/CreditsControllerTests.cs`

## Fase 4 — Cierre

- [ ] T036 [P] Agregar el módulo a la tabla de recursos del archivo de presentación del repositorio — `README.md`
- [ ] T037 [P] Actualizar el inventario de recursos y el modelo de datos de la línea base — `docs/sdd/estado-actual/ingenieria-inversa.md`, `docs/sdd/estado-actual/er-diagram.md`
- [ ] T038 Ejecutar el formato automatizado del proyecto sin cambios pendientes
- [ ] T039 Verificar la integración continua en verde
- [ ] T040 Ejecutar la verificación de convergencia entre artefactos y código

---

## Dependencias

```
T001, T002 ──┐
T003 ── T004 ── T005 ── T006 ──┬── Fase 1 ── Fase 2 ── Fase 3 ── Fase 4
             │                 │
             └─────────────────┘
```

- La fase 1 no puede empezar sin T006: sin tabla no hay consulta.
- Las fases 2 y 3 dependen de la fase 1, pero **no entre sí**. Si hiciera falta, la fotografía
  podría implementarse antes que la administración completa.
- T036 a T040 requieren que las tres historias estén integradas.

## Recuento

| Fase | Tareas | Paralelizables |
|---|---|---|
| 0 — Preparación | 6 | 2 |
| 1 — US1 | 11 | 4 |
| 2 — US2 | 11 | 4 |
| 3 — US3 | 7 | 2 |
| 4 — Cierre | 5 | 2 |
| **Total** | **40** | **14** |
