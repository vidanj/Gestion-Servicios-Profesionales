# Spec 009 — Visor de trazabilidad: registros y trazas

> **Estado:** planeación (sin implementar) · **Fecha:** 2026-09-24 ·
> **Rama:** `docs/255-spec-009-planeación-del-visor-de-trazabilidad-logs-y-trazas`
>
> **Origen:** caso de estudio de liberación y despliegue continuo, punto 2 (visor de trazabilidad:
> registros y trazas). Issue [#255](https://github.com/vidanj/Gestion-Servicios-Profesionales/issues/255).
> Continúa la tubería de trazas que la [spec 008](../008-monitoreo-metricas-alertas/spec.md) dejó
> creada y reservada para este punto.
>
> **Relacionados:** [plan](plan.md) · [research](research.md) · [modelo de datos](data-model.md) ·
> [quickstart](quickstart.md) · [tareas](tasks.md) · [análisis](analysis.md) ·
> [constitución](../../.specify/memory/constitution.md)

## 1. Contexto y valor

La aplicación ya produce todo lo que un visor de trazabilidad necesita, y no hay dónde verlo.

- **Registros.** Desde el issue #124, Serilog escribe cada evento como JSON compacto a stdout, con
  `TraceId` y `SpanId` en cada línea emitida dentro de una petición, y con redacción de secretos.
  Hoy se consultan con `docker logs | grep` o en la pestaña de registros del proveedor: sin búsqueda,
  sin filtros, sin retención y sin forma de ver dos contenedores a la vez.
- **Trazas.** La API instrumenta ASP.NET Core, `HttpClient` y el driver de PostgreSQL, y las exporta
  por OTLP cuando existe `OTEL_EXPORTER_OTLP_ENDPOINT`. El colector de la spec 008 ya las recibe,
  pero su único destino es `debug`: se imprimen en la consola del colector y se pierden.

El `TraceId` que une ambas cosas **ya existe en los datos**. El enricher de correlación lo toma de la
misma `Activity` que exporta OpenTelemetry, de modo que el identificador de una línea de registro y el
de su traza son el mismo. Falta el lugar donde esa unión se pueda usar.

**Valor entregado:** ante un error o una petición lenta, quien opera el sistema pasa de "buscar a
mano en la salida de un contenedor" a abrir el error, ver la traza completa de esa petición —qué
consultas hizo a la base, cuánto tardó cada una, dónde falló— y volver desde la traza a todos sus
registros, en un solo visor y sin adivinar qué líneas pertenecen a qué petición.

**Decisión de alcance del responsable:** el visor se arma sobre el Grafana que ya aprovisiona la
spec 008, con almacenes autoalojados (ver [research](research.md), D1), y **sin modificar el backend**
(D2). Se consume la telemetría que ya se produce; no se añade ninguna.

## 2. Historias de usuario

### US1 — Buscar los registros de la aplicación en un solo lugar (P1, MVP)

**Como** responsable de la operación, **quiero** consultar los registros de la API filtrando por
nivel, texto, intervalo de tiempo e identificador de traza, **para** diagnosticar un problema sin
entrar al contenedor ni depender de que siga vivo.

**Por qué esta prioridad:** es el cimiento de las demás. Sin registros almacenados no hay nada que
correlacionar con una traza (US3). Además entrega valor por sí sola: convierte una salida efímera en
un historial consultable.

**Prueba independiente:** levantar el entorno, generar tráfico y encontrar en el visor los inicios de
sesión fallidos del tráfico sintético filtrando solo por nivel `Warning`.

```
DADO el entorno de monitoreo y la API en contenedor
CUANDO se genera tráfico con peticiones correctas, rutas inexistentes e inicios de sesión fallidos
ENTONCES el visor muestra esas peticiones como registros de la API
Y se pueden filtrar por nivel sin configurar nada después del arranque
```

```
DADO un registro emitido dentro de una petición
CUANDO se consulta en el visor
ENTONCES su TraceId es consultable como campo
Y el TraceId no aparece como etiqueta del almacén
```

```
DADO que la API se reinicia y su contenedor cambia de identificador
CUANDO vuelve a emitir registros
ENTONCES siguen llegando al visor bajo el mismo servicio, sin intervención manual
```

### US2 — Ver la traza completa de una petición (P1)

**Como** responsable de la operación, **quiero** abrir la traza de una petición y ver cada tramo
—la petición HTTP, las llamadas salientes y las consultas a la base— con su duración, **para** saber
dónde se fue el tiempo o dónde ocurrió el fallo.

**Por qué esta prioridad:** es la otra mitad del visor y la más barata de obtener: la tubería existe,
solo le falta destino. Es independiente de US1.

**Prueba independiente:** generar tráfico, buscar en el visor las trazas de la ruta de servicios y
abrir una: debe mostrar el tramo de la petición y, debajo, los de PostgreSQL.

```
DADO el entorno de monitoreo y la API exportando trazas al colector
CUANDO se genera tráfico
ENTONCES las trazas son consultables en el visor por identificador y por duración, estado y ruta
Y una traza de una ruta que consulta la base muestra tramos del driver de PostgreSQL
```

```
DADO que el contenedor sondea /health cada 30 segundos
CUANDO pasa una hora sin tráfico
ENTONCES el almacén de trazas no contiene ninguna traza de las sondas
```

### US3 — Navegar entre un registro y su traza (P2)

**Como** responsable de la operación, **quiero** pasar de una línea de registro a su traza con un
clic, y de una traza a todos sus registros, **para** reconstruir la historia de una petición concreta
en lugar de adivinarla.

**Por qué esta prioridad:** es lo que convierte dos almacenes en un visor de **trazabilidad**. Depende
de US1 y US2.

**Prueba independiente:** desde un registro de error abrir su traza; desde esa traza volver a sus
registros y comprobar que son los mismos.

```
DADO un registro con TraceId
CUANDO se pulsa su enlace de traza
ENTONCES se abre la traza con ese mismo identificador
```

```
DADO una traza abierta
CUANDO se pide ver sus registros
ENTONCES se muestran todos los registros con ese TraceId, y ningún otro
```

```
DADO un TraceId copiado de otra fuente (por ejemplo, una fila de la bitácora de auditoría)
CUANDO se pega en el tablero de trazabilidad
ENTONCES el tablero muestra juntos los registros y la traza de esa petición
```

### US4 — Levantar y verificar el visor con los guiones del proyecto (P3)

**Como** integrante del equipo, **quiero** que el visor se levante, se apague y se verifique con los
mismos guiones que el resto del monitoreo, **para** que funcione igual en cualquier equipo y en la
integración continua.

**Por qué esta prioridad:** es reproducibilidad. Sin ella el visor funciona en un equipo y en ningún
otro, pero el visor ya entrega valor sin ella.

**Prueba independiente:** en un equipo limpio, `monitoreo-up`, `generar-trafico` y
`verificar-monitoreo` terminan con código cero; apagar Loki hace que la verificación falle y diga qué
falta.

```
DADO el entorno levantado con los guiones del proyecto
CUANDO se ejecuta la verificación después de generar tráfico
ENTONCES comprueba que un TraceId tomado de un registro existe como traza
Y comprueba que ningún registro contiene las contraseñas del tráfico sintético
Y termina con error, indicando qué falta, si alguna de las dos cosas no se cumple
```

## 3. Requisitos funcionales

### US1 — Registros

- **FR-001** Los registros de la API se recolectan desde la salida estándar de su contenedor, sin
  modificar ningún archivo bajo `backend/`.
- **FR-002** Solo se recolectan los registros del servicio de la API. Se identifica igual en el
  entorno local (servicio de composición `api`) y en la integración continua (contenedor `api`).
- **FR-003** Cada registro se interpreta como JSON. El nivel se extrae del campo del formato compacto
  y, cuando falta, se asume `Information`, que es exactamente lo que ese formato omite.
- **FR-004** `TraceId` y `SpanId` se guardan como metadatos consultables del registro, **no** como
  etiquetas indexadas.
- **FR-005** Las únicas etiquetas indexadas son `servicio` y `nivel`, ambas de dominio cerrado.
- **FR-006** Una línea que no sea JSON se conserva sin nivel extraído; no se descarta.
- **FR-007** El recolector conserva su posición de lectura entre reinicios, para no duplicar ni
  perder registros al reiniciarlo.
- **FR-008** Los registros se retienen 7 días y después se eliminan automáticamente.
- **FR-009** El origen de datos de registros se aprovisiona de forma declarativa en Grafana, con
  identificador fijo, y permite filtrar por nivel, texto, intervalo y `TraceId`.

### US2 — Trazas

- **FR-010** La tubería de trazas del colector exporta al almacén de trazas; el destino provisional
  `debug` se retira.
- **FR-011** Las trazas se retienen 7 días. No se aplica muestreo: se conservan todas.
- **FR-012** Las trazas se pueden buscar por identificador y por duración mínima, estado de error y
  ruta.
- **FR-013** El receptor OTLP del almacén de trazas no se publica en el equipo anfitrión: los puertos
  4317 y 4318 del anfitrión siguen siendo del colector.
- **FR-014** El origen de datos de trazas se aprovisiona en Grafana con identificador fijo.
- **FR-015** Las sondas de salud siguen sin generar trazas; este requisito protege un comportamiento
  existente y se verifica, no se implementa.

### US3 — Correlación y tablero

- **FR-016** Cada registro con `TraceId` ofrece un enlace que abre su traza.
- **FR-017** Cada traza ofrece un enlace que muestra sus registros, filtrados por su identificador.
- **FR-018** Existe un tablero "Trazabilidad" aprovisionado desde un archivo versionado, no editable
  desde la interfaz, igual que los de la spec 008.
- **FR-019** El tablero tiene un campo `TraceId`; al llenarlo muestra juntos los registros y la traza
  de esa petición.
- **FR-020** El tablero muestra además el volumen de registros por nivel, los registros de error y
  advertencia recientes y las trazas más lentas del intervalo.
- **FR-021** El quickstart documenta cómo llegar al visor desde una fila de la bitácora de auditoría
  copiando su `TraceId`.

### US4 — Operación y verificación

- **FR-022** `monitoreo-up` levanta los servicios nuevos, espera a que respondan sanos e imprime sus
  direcciones, en sus dos variantes (`.sh` y `.ps1`).
- **FR-023** La purga de `monitoreo-down` incluye los volúmenes nuevos y conserva su confirmación
  explícita, en sus dos variantes.
- **FR-024** `verificar-monitoreo` comprueba que el almacén de registros, el de trazas y el
  recolector responden sanos, que llegan registros de la API y que tienen nivel y `TraceId`
  extraídos, en sus dos variantes.
- **FR-025** La verificación toma un `TraceId` de un registro reciente y comprueba que existe como
  traza. Es la prueba de extremo a extremo de la correlación.
- **FR-026** La verificación comprueba que ningún registro almacenado contiene las contraseñas que
  usa el tráfico sintético.
- **FR-027** La integración continua valida la configuración de las tres herramientas nuevas y
  comprueba que los tableros solo referencian los orígenes de datos aprovisionados.
- **FR-028** El trabajo de humo de la integración continua ejecuta la verificación extendida.

### Transversales

- **FR-029** Ningún archivo bajo `backend/` cambia.
- **FR-030** Todas las imágenes nuevas tienen versión fijada; ninguna etiqueta móvil.
- **FR-031** Los puertos nuevos se documentan y no chocan con los del proyecto.
- **FR-032** Si el recolector necesita el socket de Docker, se monta en solo lectura y el riesgo
  queda documentado en el README del entorno.
- **FR-033** El README del entorno de monitoreo y la sección de observabilidad del README raíz se
  actualizan en el mismo PR de implementación.
- **FR-034** Cada servicio nuevo declara un límite de memoria.

## 4. Criterios de éxito

| ID | Criterio | Cómo se mide |
|---|---|---|
| SC-001 | Partiendo de un `TraceId`, se ven sus registros y su traza en menos de 1 minuto | Recorrido 3 del [quickstart](quickstart.md), cronometrado |
| SC-002 | El 100 % de los `TraceId` muestreados de registros de la API resuelven a una traza | Verificación automatizada (FR-025) sobre una muestra de al menos 5 identificadores |
| SC-003 | Un registro es consultable 30 s después de emitirse, y una traza 60 s después | Marca de tiempo de la petición contra la primera consulta que la encuentra |
| SC-004 | Cero registros con las contraseñas del tráfico sintético | Verificación automatizada (FR-026) |
| SC-005 | El almacén de registros tiene como máximo 7 flujos para la API | Consulta de series del almacén: un flujo por cada combinación de `servicio` y `nivel` |
| SC-006 | Cero cambios bajo `backend/` | `git diff --stat` del PR de implementación contra su base |
| SC-007 | Integración continua en verde con la verificación extendida | Ejecución del flujo de monitoreo en el PR de implementación |

## 5. Casos límite

- **Registros sin `TraceId`.** Las tareas en segundo plano y el arranque no tienen traza, y el
  enricher no inventa una. Esos registros se almacenan y se consultan; simplemente no ofrecen enlace.
- **La API se reinicia.** Cambia el identificador del contenedor, no el servicio: la identificación
  por servicio (FR-002) la sigue sin intervención.
- **El recolector se reinicia.** Retoma desde su última posición (FR-007).
- **El almacén de registros cae un rato.** Los registros siguen en el archivo de Docker y se envían al
  recuperarse, dentro de los reintentos del recolector. Una caída prolongada puede perder registros:
  se acepta, porque este visor es diagnóstico, no evidencia. La evidencia de quién hizo qué es
  `UserLog` en PostgreSQL.
- **La API se ejecuta con `dotnet run`, fuera de contenedor.** Sus trazas llegan si se define la
  variable del colector, pero sus registros no, porque no pasan por Docker. Queda documentado para que
  la asimetría no se confunda con una falla.
- **Una línea que no es JSON**, como un aviso del runtime previo a Serilog. Se conserva (FR-006).

## 6. Fuera de alcance

| Qué | Por qué |
|---|---|
| Enlace desde la bitácora de auditoría (`/usuarios/logs`) hacia el visor | Es la pantalla del punto 3 (spec 010). Aquí se documenta el camino manual por `TraceId` (FR-021) |
| Registros y trazas del servicio publicado | Mismo límite que la spec 008, §5.3: el contenedor publicado no puede empujar a un colector local, y exponer ese colector sería abrir un receptor público |
| Alertas basadas en registros | Las alertas del proyecto son las de la spec 008; añadir un segundo motor de reglas no lo pide este punto |
| Métricas derivadas de trazas (grafo de servicios, métricas de tramos) | Con un solo servicio el grafo no aporta, y las métricas de latencia ya existen en la spec 008 |
| Registros del frontend | El frontend no emite registros estructurados ni trazas hoy |
| Cualquier cambio al backend | Decisión del responsable (research, D2) |

## 7. Clarificaciones

| Pregunta | Resolución | Razón |
|---|---|---|
| ¿Servicio externo (SaaS) o visor propio? | Autoalojado, sobre el Grafana existente | Reutiliza el entorno de la spec 008, no saca datos del proyecto y no depende de un periodo de prueba |
| ¿Cómo llegan los registros al almacén? | Leyéndolos de Docker, sin tocar el backend | Menor trabajo y cero riesgo sobre un backend que compila con advertencias como errores |
| ¿El enlace desde la bitácora de auditoría es de esta spec? | No. Se documenta la búsqueda manual | La pantalla pertenece al punto 3; tocarla aquí mezclaría dos entregas |
| ¿Numeración? | 009 para trazabilidad, 010 para auditoría | Acuerdo del equipo |
| ¿Rama base? | `ci/251-monitoreo-y-despliegue`; PR hacia `dev`, sin fusionar hasta que lo estén #252 y #253 | La spec depende del entorno de la 008 |

## 8. Supuestos

- El entorno de monitoreo de la spec 008 existe y funciona tal como lo deja el PR #253.
- Docker usa su controlador de registros por omisión (`json-file`) en los equipos del equipo y en la
  integración continua.
- Los equipos de trabajo ejecutan Docker Desktop con contenedores Linux, o Docker nativo en Linux.
