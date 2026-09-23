# Modelo de datos — Spec 008

> Aquí no hay entidades de base de datos: esta spec **no cambia el esquema** y no genera ninguna
> migración. Lo que se modela son las tres cosas que sí tienen forma y contrato: el **catálogo de
> métricas**, la **alerta** y las **variables de entorno**.

---

## 1. Catálogo de métricas

La traducción de nombres es mecánica y la hace el colector: los puntos pasan a guiones bajos, se
añade el sufijo de la unidad y los contadores reciben `_total`. Un histograma produce tres series
(`_bucket`, `_sum`, `_count`).

### 1.1 Peticiones y servidor (instrumentación automática, ya activa)

| Nombre en el sistema | Nombre consultable | Tipo | Etiquetas principales | Para qué sirve |
|---|---|---|---|---|
| `http.server.request.duration` | `http_server_request_duration_seconds_{bucket,sum,count}` | Histograma (s) | método, código de estado, ruta, tipo de error | **La métrica central.** De ella salen latencia p95, proporción de errores y volumen. Su `_count` hace de contador de peticiones |
| `http.server.active_requests` | `http_server_active_requests` | Contador bidireccional | método, esquema | Peticiones en vuelo: saturación instantánea |
| `kestrel.active_connections` | `kestrel_active_connections` | Contador bidireccional | — | Conexiones abiertas, haya o no petición en curso |
| `kestrel.queued_requests` | `kestrel_queued_requests` | Contador bidireccional | — | Trabajo aceptado y todavía no atendido |
| `kestrel.rejected_connections` | `kestrel_rejected_connections_total` | Contador | — | Se alcanzó el límite de conexiones: saturación dura |
| `aspnetcore.routing.match_attempts` | `aspnetcore_routing_match_attempts_total` | Contador | resultado del emparejamiento, ruta | Tráfico hacia rutas inexistentes: rastreo o escaneo de terceros |
| `aspnetcore.diagnostics.exceptions` | `aspnetcore_diagnostics_exceptions_total` | Contador | tipo de excepción | Excepciones no controladas que llegan al middleware |
| `aspnetcore.rate_limiting.requests` | `aspnetcore_rate_limiting_requests_total` | Contador | resultado, política | **Rechazos del limitador de autenticación.** Requiere registrar su medidor explícitamente (research, D5) |

### 1.2 Tiempo de ejecución de la plataforma

Nombres `dotnet.*`, no `process.runtime.dotnet.*` (research, D4).

| Nombre en el sistema | Nombre consultable | Tipo | Para qué sirve |
|---|---|---|---|
| `dotnet.process.memory.working_set` | `dotnet_process_memory_working_set_bytes` | Medidor | **La más importante del grupo**: el plan de alojamiento tiene memoria acotada y el reinicio por memoria es la caída más frecuente |
| `dotnet.process.cpu.time` | `dotnet_process_cpu_time_seconds_total` | Contador | Consumo real frente al tráfico atendido |
| `dotnet.gc.collections` | `dotnet_gc_collections_total` | Contador | Presión de memoria; el crecimiento sostenido de la generación mayor delata una fuga |
| `dotnet.gc.pause.time` | `dotnet_gc_pause_time_seconds_total` | Contador | Tiempo con la aplicación detenida: explica latencias sin causa aparente |
| `dotnet.thread_pool.queue.length` | `dotnet_thread_pool_queue_length` | Medidor | **Trabajo pendiente**: si crece, la aplicación va por detrás de la demanda y la latencia subirá después |
| `dotnet.thread_pool.thread.count` | `dotnet_thread_pool_thread_count` | Medidor | Hilos en uso |
| `dotnet.monitor.lock_contentions` | `dotnet_monitor_lock_contentions_total` | Contador | Contención de bloqueos |
| `dotnet.exceptions` | `dotnet_exceptions_total` | Contador | Excepciones lanzadas, incluidas las capturadas: las del controlador de base de datos delatan una caída antes que la sonda |

### 1.3 Negocio (medidor propio)

Medidor `SistemaServicios.API.Negocio`. Todos los valores de etiqueta están **cerrados** y se
declaran aquí; ninguno se construye a partir de datos de entrada.

| Nombre en el sistema | Nombre consultable | Tipo | Etiqueta y valores posibles | Origen en el código |
|---|---|---|---|---|
| `gsp.solicitudes.creadas` | `gsp_solicitudes_creadas_total` | Contador | `resultado`: `creada`, `servicio_no_disponible` | `ServiceRequestService.CreateRequestAsync` |
| `gsp.solicitudes.cambios_estado` | `gsp_solicitudes_cambios_estado_total` | Contador | `estado_origen` y `estado_destino`: `Pending`, `Accepted`, `InProgress`, `Completed`, `Cancelled` · `resultado`: `aceptada`, `rechazada`, `no_autorizada` | `ServiceRequestService.UpdateStatusAsync` |
| `gsp.autenticacion.intentos` | `gsp_autenticacion_intentos_total` | Contador | `resultado`: `exito`, `credenciales_invalidas`, `cuenta_inactiva` | `AuthService.LoginAsync` |
| `gsp.usuarios.registrados` | `gsp_usuarios_registrados_total` | Contador | `resultado`: `creado`, `correo_duplicado` | `AuthService.RegisterAsync` |
| `gsp.respaldos.ejecutados` | `gsp_respaldos_ejecutados_total` | Contador | `resultado`: `exito`, `herramienta_ausente`, `fallo_ejecucion`, `configuracion_incompleta` | `BackupService.GenerateBackupAsync` |
| `gsp.respaldos.duracion` | `gsp_respaldos_duracion_seconds_{bucket,sum,count}` | Histograma (s) | `resultado`: `exito`, `fallo` | `BackupService.GenerateBackupAsync` |

**Nota sobre el inicio de sesión.** El servicio devuelve deliberadamente el **mismo** mensaje para
credenciales incorrectas y para cuenta desactivada, para no revelar cuál de las dos ocurrió. La
métrica sí distingue ambos casos, y eso **no rompe** esa decisión: la respuesta que ve el cliente
sigue siendo idéntica, y la métrica es agregada, interna y sin identificadores. Es justamente lo que
permite responder "¿los usuarios no entran porque se equivocan, o porque sus cuentas están
desactivadas?" sin filtrar nada.

**Nota sobre los cambios de estado.** Las transiciones válidas del sistema son seis
(`Pending→Accepted`, `Pending→Cancelled`, `Accepted→InProgress`, `Accepted→Cancelled`,
`InProgress→Completed`, `InProgress→Cancelled`); `Completed` y `Cancelled` son terminales. Por eso un
volumen apreciable de `rechazada` no es tráfico hostil: significa que la interfaz está ofreciendo
transiciones que el servidor no admite. El caso "solicitud no encontrada" **no** se cuenta aquí,
porque no tiene estado de origen y la métrica de peticiones ya lo registra como 404.

**Cardinalidad.** Peor caso de la métrica de cambios de estado: 5 × 5 × 3 = 75 series, y en la
práctica muchas menos porque la mayoría de las combinaciones nunca ocurre. El resto de las métricas
de negocio aportan entre 2 y 4 series cada una.

### 1.4 Sondeo externo

| Nombre consultable | Tipo | Etiquetas | Para qué sirve |
|---|---|---|---|
| `probe_success` | Medidor (0/1) | ambiente, sonda | Disponibilidad. Es la base de los dos primeros niveles de servicio |
| `probe_duration_seconds` | Medidor | ambiente, sonda | Tiempo de respuesta visto desde fuera; en el servicio publicado, mide el arranque en frío |
| `probe_http_status_code` | Medidor | ambiente, sonda | Distingue un 503 de la sonda de un error de red |

### 1.5 Etiquetas que se añaden solas

| Etiqueta | La pone | Contenido |
|---|---|---|
| `job` | El recolector | Qué grupo de objetivos produjo la serie |
| `instance` | El recolector | Qué objetivo concreto |
| `otel_scope_name` | El colector | Qué medidor emitió la métrica |
| `ambiente` | Configuración del recolector | `local` o `produccion` |

La identidad del servicio y su versión **no** se copian a cada serie: viajan en una serie aparte de
información del objetivo. Copiarlas multiplicaría la cardinalidad sin añadir capacidad de consulta.

---

## 2. La alerta

| Campo | Descripción | Obligatorio |
|---|---|---|
| `alert` | Nombre en notación de sujeto y estado (`ApiNoDisponible`), no de causa | Sí |
| `expr` | Expresión evaluada; si comparte lógica con un tablero, se toma de una regla de registro | Sí |
| `for` | Cuánto debe sostenerse antes de disparar. Es el parámetro que absorbe el arranque en frío | Sí |
| `labels.severidad` | `critica` (exige acción ahora) o `advertencia` (exige acción, no ahora) | Sí |
| `labels.componente` | `api`, `base-de-datos`, `observabilidad`, `seguridad`, `respaldos` | Sí |
| `annotations.resumen` | Una línea, en español, que diga qué ocurre | Sí |
| `annotations.descripcion` | Qué significa y **por qué ese umbral y ese tiempo de espera** | Sí |
| `annotations.accion` | Qué hacer a continuación | Sí |

Una alerta sin `accion` no está terminada: quien la recibe a las tres de la mañana necesita el
siguiente paso, no solo el diagnóstico.

### Estados y transiciones

```text
inactiva ──(la expresión se cumple)──► pendiente ──(se sostiene durante `for`)──► disparada
    ▲                                      │                                          │
    └──────(deja de cumplirse)─────────────┴──────────────────────────────────────────┘
```

El estado *pendiente* es el que evita que un pico de dos segundos despierte a nadie.

---

## 3. Variables de entorno

| Variable | Ámbito | Sensible | Para qué |
|---|---|---|---|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Aplicación | No | Dirección del colector. **Ya existe**; hoy está vacía, y esta spec le da por fin un destino |
| `GRAFANA_ADMIN_PASSWORD` | Entorno de monitoreo | **Sí** | Contraseña del administrador del tablero. Nueva |
| `RENDER_DEPLOY_HOOK_URL` | Secreto de integración continua | **Sí** | Dispara el despliegue. Nunca aparece en registros de ejecución |
| `RENDER_SERVICE_URL` | Variable de integración continua | No | Dirección pública a sondear tras desplegar |
| `RENDER_API_KEY` | Secreto de integración continua | **Sí** | Opcional: distingue un fallo de construcción de un fallo de arranque |
| `RENDER_SERVICE_ID` | Variable de integración continua | No | Opcional, acompaña a la anterior |

Las dos primeras se documentan en `.env.example` con valores de ejemplo. Las cuatro restantes viven
en secretos y variables de la plataforma de integración continua, nunca en el repositorio
(constitución, *Seguridad y Datos*).
