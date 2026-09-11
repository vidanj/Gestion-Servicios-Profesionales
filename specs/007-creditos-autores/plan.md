# Plan — Spec 007, módulo de créditos de autores

> **Entrada:** [spec.md](spec.md) y la [constitución](../../.specify/memory/constitution.md)
> **Salida:** este plan, [research.md](research.md), [data-model.md](data-model.md),
> [contracts/](contracts/credits-api.md), [quickstart.md](quickstart.md)
>
> Este documento decide el **cómo**. El **qué** y el **por qué** están en el spec.

---

## 1. Contexto técnico

| Aspecto | Decisión |
|---|---|
| Lenguaje y plataforma | La del backend vigente del proyecto |
| Acceso a datos | El ORM y el proveedor de PostgreSQL ya configurados |
| Arquitectura | Controlador → servicio → repositorio → contexto de datos, sin excepciones |
| Autenticación | El esquema por token vigente; política por rol para escritura |
| Almacenamiento de archivos | La abstracción de almacenamiento existente |
| Pruebas | El marco de pruebas vigente para unitarias y de integración |
| Formato | La herramienta de formato ya integrada en el flujo de integración continua |
| Dependencias nuevas | Ninguna |

## 2. Verificación contra la constitución

> **Aviso:** esta tabla se redactó a partir de los principios referidos en la documentación del
> proyecto. Antes de aprobar el plan hay que contrastarla con el texto vigente de la constitución,
> ya que su versión puede haber cambiado.

| Principio | Cómo lo cumple este plan | Estado |
|---|---|---|
| I — Aprobación previa a implementar | Este PR entrega solo planeación; no hay código ni migraciones aplicadas | Cumple |
| II — Separación por capas | El controlador solo mapea peticiones y respuestas; el acceso a datos vive en el repositorio | Cumple |
| III — Contratos por objetos de transferencia | Entrada y salida por objetos de transferencia propios del módulo; la entidad no se expone | Cumple |
| IV — Cambios de esquema solo por migración | Una migración generada con la herramienta del ORM; sin instrucciones manuales | Cumple |
| V — Pruebas por requisito | Cada requisito funcional tiene al menos una tarea de prueba unitaria o de integración | Cumple |
| VI — Formato automatizado | El módulo se formatea con la herramienta del proyecto antes del PR | Cumple |
| VII — Restricción de dependencias | No se agrega ningún paquete; es criterio de éxito verificable | Cumple |

**Violaciones que requieran justificación:** ninguna.

## 3. Estructura prevista

```text
backend/SistemaServicios.API/
├── Models/
│   └── Credit.cs                        # entidad
├── DTOs/Credits/
│   ├── CreditDto.cs                     # salida pública
│   ├── CreditsPageDto.cs                # título + mensaje + autores
│   ├── CreateCreditDto.cs               # entrada de alta
│   └── UpdateCreditDto.cs               # entrada de actualización
├── Interfaces/
│   ├── ICreditRepository.cs
│   └── ICreditService.cs
├── Repositories/
│   └── CreditRepository.cs
├── Services/
│   └── CreditService.cs
├── Controllers/
│   └── CreditsController.cs
├── Configuration/
│   └── CreditsOptions.cs                # título y mensaje desde configuración
├── Data/AppDbContext.cs                 # (modificado) conjunto de entidades e índices
├── Extensions/
│   └── ApplicationServiceExtensions.cs  # (modificado) registro de dependencias
└── Migrations/
    └── <marca>_AddCredits.cs            # generada, no escrita a mano

backend/SistemaServicios.Tests/
├── Unit/
│   └── CreditServiceTests.cs
└── Integration/
    └── CreditsControllerTests.cs
```

## 4. Decisiones de diseño

Las decisiones y sus alternativas descartadas están en [research.md](research.md):

| # | Decisión |
|---|---|
| D1 | Autores en base de datos |
| D2 | Fotografías en el almacenamiento de archivos existente |
| D3 | Mensaje del equipo en configuración |
| D4 | Baja lógica |
| D5 | Orden explícito con desempate por identificador |
| D6 | Un solo recurso de lectura con título, mensaje y autores |
| D7 | Sin integración con la bitácora en esta iteración |
| D8 | Sin dependencias nuevas |

## 5. Fases de ejecución

| Fase | Contenido | Criterio de salida |
|---|---|---|
| 0 — Preparación | Entidad, configuración del contexto de datos, migración generada | La migración se aplica y revierte sin error en una base limpia |
| 1 — Historia US1 | Objetos de transferencia, repositorio, servicio y recurso de lectura pública | Un visitante sin sesión obtiene los créditos; pruebas en verde |
| 2 — Historia US2 | Recursos de alta, actualización, baja y reordenamiento | Un rol no administrador recibe respuesta de acceso denegado; pruebas en verde |
| 3 — Historia US3 | Asociación de fotografía con validación por contenido | Un archivo falsificado se rechaza y no se almacena |
| 4 — Cierre | Documentación, formato, integración continua, verificación de convergencia | Inventario de recursos actualizado; sin diferencias entre artefactos y código |

Cada fase corresponde a una historia entregable por separado. La fase 1 es el mínimo viable: si el
trabajo se detuviera ahí, el módulo ya cumpliría su propósito.

## 6. Riesgos

| Riesgo | Probabilidad | Mitigación |
|---|---|---|
| Interpretar que un autor es un usuario del sistema | Media | El modelo de datos lo declara explícitamente; el objeto de transferencia no expone identificadores de usuario |
| Resultado no determinista al ordenar | Media | Desempate obligatorio por identificador, con prueba que lo verifique |
| Fuga de detalle de excepción en respuestas de error | Media | Es un requisito funcional con prueba propia; el proyecto ya arrastra este defecto en otros controladores |
| Borrar un archivo referenciado por un autor | Baja | Regla de restricción declarada explícitamente en la relación |
| Enlace con esquema arbitrario en el campo de perfil | Baja | Validación de esquema antes de persistir |

## 7. Lo que este plan no decide

- El diseño visual ni la maquetación de la página de créditos.
- El contenido concreto de los autores; se carga por los recursos de administración.
- Si el mensaje del equipo debe volverse editable desde la interfaz. Esa decisión se toma si aparece
  la necesidad, y revierte D3.
