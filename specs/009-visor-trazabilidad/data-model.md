# Modelo de datos — Spec 009

> Qué datos recorre el visor, cómo se transforman en cada salto y qué parámetros los gobiernan.
> No hay entidades nuevas en la base de datos ni migraciones.
>
> **Relacionados:** [spec](spec.md) · [research](research.md) · [plan](plan.md)

---

## 1. El registro

### 1.1 Lo que emite la API hoy (no cambia)

Serilog escribe una línea JSON por evento, en formato compacto, a stdout. Campos relevantes:

| Campo | Contenido | Siempre presente |
|---|---|---|
| `@t` | Marca de tiempo | Sí |
| `@mt` | Plantilla del mensaje | Sí |
| `@l` | Nivel | **No:** se omite cuando es `Information` (research, D4) |
| `@x` | Excepción | Solo si la hay |
| `TraceId` | Identificador de traza, 32 caracteres hexadecimales | Solo dentro de una petición |
| `SpanId` | Identificador del tramo, 16 caracteres hexadecimales | Solo dentro de una petición |
| `Application` | `SistemaServicios.API` | Sí |
| `Environment` | Ambiente de ejecución | Sí |
| `RequestMethod`, `RequestPath`, `StatusCode`, `Elapsed`, `ClientIp` | Evento de petición | Solo en el evento de petición |

`TraceId` y `SpanId` los añade `ActivityCorrelationEnricher` a partir de `Activity.Current`, que es la
misma actividad que exporta OpenTelemetry. **Por eso coinciden con los de la traza sin ninguna
traducción.**

> La tarea T002 confirma estos nombres sobre una línea real antes de escribir la configuración del
> recolector. En particular, verifica si el formato añade también `@tr` y `@sp`; si lo hace, se
> sigue usando `TraceId`, porque lo controla el proyecto.

### 1.2 Lo que guarda Loki

| Parte | Origen | Tipo en Loki | Dominio |
|---|---|---|---|
| `servicio` | Etiqueta de composición o nombre del contenedor | Etiqueta indexada | `api` |
| `nivel` | `@l`, o `Information` si falta | Etiqueta indexada | `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`, o ausente en líneas que no son JSON |
| `TraceId` | Campo del JSON | Metadato estructurado | Ilimitado: por eso **no** es etiqueta |
| `SpanId` | Campo del JSON | Metadato estructurado | Ilimitado |
| Línea | La línea JSON completa, sin alterar | Contenido | — |

**Flujos máximos para la API:** un flujo por cada combinación de etiquetas, es decir, como máximo 7
(SC-005). La línea se guarda completa y sin reescribir: cualquier campo que no se extrae sigue
consultable con el analizador JSON de LogQL.

### 1.3 Política de etiquetas

Se hereda de la spec 008 y se aplica igual: una etiqueta solo es válida si se puede escribir de
antemano la lista de todos sus valores posibles.

**Prohibido como etiqueta de Loki:** `TraceId`, `SpanId`, ruta con parámetros, dirección IP, correo,
identificador de usuario, código de estado, mensaje y nombre del contenedor (cambia en cada reinicio).

## 2. La traza

### 2.1 Lo que exporta la API hoy (no cambia)

| Instrumentación | Qué produce | Nota |
|---|---|---|
| ASP.NET Core | Un tramo raíz por petición HTTP | Las rutas `/health` están excluidas por filtro |
| `HttpClient` | Un tramo por llamada saliente | — |
| Npgsql | Un tramo por comando a PostgreSQL | Sustituye a la instrumentación de EF Core, que sigue en preestreno |

Recurso: `service.name = SistemaServicios.API`.

### 2.2 Recorrido

```
API ──OTLP──► colector (4317) ──OTLP──► Tempo (red interna) ◄──consulta── Grafana
```

El colector no transforma las trazas; solo cambia su destino de `debug` a Tempo. Pasan por el mismo
limitador de memoria y el mismo agrupador que ya tiene la tubería.

## 3. La correlación

| Dirección | Mecanismo | Configurado en |
|---|---|---|
| Registro → traza | Campo derivado sobre el metadato `TraceId` que enlaza al origen `gsp-tempo` | Origen de datos de Loki |
| Traza → registros | Consulta a `gsp-loki` filtrada por el identificador de la traza | Origen de datos de Tempo |
| Identificador externo → ambos | Variable `TraceId` del tablero | Tablero "Trazabilidad" |

La clave común es **un único valor**: el identificador de traza W3C de 32 caracteres hexadecimales,
tal como lo escribe `Activity.TraceId.ToString()`.

## 4. Parámetros de configuración

### 4.1 Loki

| Parámetro | Valor | Efecto |
|---|---|---|
| Modo | Un solo proceso | Sin componentes distribuidos |
| Almacenamiento | Disco local, volumen nombrado | Sobrevive a reinicios del contenedor |
| Esquema | El vigente de la rama 3.x, con índice TSDB | Requisito para metadatos estructurados |
| Metadatos estructurados | Permitidos | Donde viven `TraceId` y `SpanId` |
| Retención | 7 días, con compactador activo | Sin compactador, la retención declarada no borra nada |
| Autenticación multiinquilino | Desactivada | Un solo inquilino local |
| Puerto publicado | 3100 | Consulta del guion de verificación y diagnóstico |
| Límite de memoria | Declarado en la composición | FR-034 |

### 4.2 Tempo

| Parámetro | Valor | Efecto |
|---|---|---|
| Modo | Monolítico | Sin Kafka (research, D5) |
| Recepción | OTLP por gRPC, solo en la red interna | No choca con el colector (research, D6) |
| Almacenamiento | Disco local, volumen nombrado | — |
| Retención de bloques | 7 días | Igual que los registros (research, D7) |
| Muestreo | Ninguno | Se conservan todas las trazas |
| Puerto publicado | 3200 | Consulta por identificador del guion de verificación |
| Límite de memoria | Declarado en la composición | FR-034 |

### 4.3 Alloy

| Parámetro | Valor | Efecto |
|---|---|---|
| Descubrimiento | API de Docker por el socket, en solo lectura | Conoce el servicio de cada contenedor (research, D2 y D9) |
| Filtro | Solo el servicio `api` | Research, D8 |
| Procesamiento | Analizar JSON, extraer nivel con valor por omisión y guardar `TraceId` y `SpanId` como metadatos | FR-003, FR-004 |
| Posiciones de lectura | Persistidas en un volumen nombrado | FR-007 |
| Destino | Loki por la red interna | — |
| Puerto publicado | 12345 | Interfaz de diagnóstico del recolector |
| Límite de memoria | Declarado en la composición | FR-034 |

### 4.4 Colector (cambio sobre la spec 008)

| Parámetro | Antes | Después |
|---|---|---|
| Exportador de la tubería `traces` | `debug` | OTLP hacia Tempo, sin TLS dentro de la red interna |
| Comentario de la tubería | "Punto 2, no se desarrolla en esta entrega" | Explica el destino y remite a esta spec |

### 4.5 Grafana (cambio sobre la spec 008)

| Parámetro | Valor | Efecto |
|---|---|---|
| Origen de datos de registros | `gsp-loki`, aprovisionado | FR-009 |
| Origen de datos de trazas | `gsp-tempo`, aprovisionado | FR-014 |
| Origen predeterminado | Sigue siendo `gsp-prometheus` | Los tableros de la 008 no cambian |
| Tablero nuevo | `gsp-trazabilidad.json`, no editable | FR-018 |

## 5. Tablero "Trazabilidad"

| Panel | Origen | Responde |
|---|---|---|
| Campo `TraceId` (variable de texto) | — | "Traigo un identificador de otra parte" |
| Registros de la petición | `gsp-loki`, filtrado por la variable | "¿Qué dijo la aplicación en esta petición?" |
| Traza de la petición | `gsp-tempo`, por la variable | "¿Dónde se fue el tiempo?" |
| Volumen de registros por nivel | `gsp-loki` | "¿Está subiendo el ruido o los errores?" |
| Errores y advertencias recientes | `gsp-loki` | "¿Qué falló últimamente?" |
| Trazas más lentas del intervalo | `gsp-tempo` | "¿Qué peticiones hay que mirar primero?" |

## 6. Puertos y volúmenes nuevos

| Servicio | Puerto en el anfitrión | Volumen |
|---|---|---|
| Loki | 3100 | `gsp_loki_datos` |
| Tempo | 3200 (4317 solo interno) | `gsp_tempo_datos` |
| Alloy | 12345 | `gsp_alloy_datos` |

## 7. Variables de entorno

Ninguna nueva. La API ya recibe `OTEL_EXPORTER_OTLP_ENDPOINT` de la superposición de la spec 008, y
ninguna herramienta nueva necesita credenciales: solo son accesibles desde el equipo local y Grafana
sigue siendo la única interfaz con usuario.
