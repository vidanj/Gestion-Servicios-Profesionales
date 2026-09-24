# Research — Spec 009

> Decisiones de diseño del visor de trazabilidad, con sus alternativas descartadas.
> **Fecha de las consultas:** 2026-09-24. Las versiones citadas se vuelven a confirmar en la tarea
> T001 antes de fijarlas.
>
> **Relacionados:** [spec](spec.md) · [plan](plan.md) · [modelo de datos](data-model.md)

---

## D1 — Loki y Tempo sobre el Grafana existente

**Pregunta:** ¿dónde se almacenan y se consultan registros y trazas?

**Decisión:** Loki para los registros y Tempo para las trazas, autoalojados en el mismo archivo de
composición que el resto del monitoreo, y consultados desde el Grafana que ya aprovisiona la
spec 008.

**Razones:**

1. **Grafana ya está, y ya se aprovisiona como código.** Añadir dos orígenes de datos y un tablero
   sigue exactamente el patrón de la spec 008: archivos versionados, edición bloqueada, identificadores
   fijos. No hay una segunda interfaz que aprender ni que asegurar.
2. **La correlación es nativa.** Grafana enlaza un campo de un registro de Loki con una traza de Tempo,
   y una traza de Tempo con una consulta de Loki, por configuración del origen de datos. Es
   precisamente el requisito central (US3), y no exige escribir código.
3. **Son ligeros en modo de un solo proceso.** Loki indexa solo etiquetas, no el contenido; Tempo en
   modo monolítico no necesita cola de mensajes. Ambos caben en un equipo de trabajo junto al resto
   del entorno.
4. **Tempo recibe OTLP.** El colector ya habla OTLP con la API; hacia Tempo solo cambia el destino.

**Alternativas descartadas:**

| Alternativa | Por qué no |
|---|---|
| Elasticsearch u OpenSearch con Kibana | Máquina virtual de Java con varios GB de memoria, una segunda interfaz de visualización y un índice de texto completo que este volumen no necesita |
| Jaeger | Resuelve trazas, no registros, y trae su propia interfaz: habría que unir dos visores en lugar de uno |
| Servicio externo (Datadog, Grafana Cloud y similares) | Descartado por el responsable: exige cuenta, sacar datos del proyecto y depender de un periodo de prueba. Su única ventaja real —recibir telemetría del servicio publicado— queda fuera de alcance igual que en la spec 008 |
| Visor propio en el frontend | Necesitaría un almacén de todos modos, una vista de cascada de tramos que el Principio VII no permite añadir como librería, y se mezclaría con la pantalla de auditoría del punto 3 |

## D2 — Los registros se leen de Docker, sin tocar el backend

**Pregunta:** ¿cómo llegan los registros de la API a Loki?

**Decisión:** un recolector (Grafana Alloy) descubre los contenedores por la API de Docker, lee la
salida del contenedor de la API y la envía a Loki. El backend no cambia.

**Razones:**

1. **Decisión del responsable: el camino con menos trabajo y sin tocar el backend.** La API ya
   escribe JSON a stdout y Docker ya lo guarda; solo falta alguien que lo lea.
2. **El descubrimiento da el nombre del servicio.** Leer por la API de Docker entrega la etiqueta de
   composición y el nombre del contenedor, con los que se identifica a la API igual en local y en la
   integración continua (FR-002). Leer los archivos de Docker directamente solo da un identificador
   de contenedor que cambia en cada reinicio.
3. **Conserva el principio del README.** "JSON a stdout, nunca a archivo" sigue siendo verdad: el
   visor es un consumidor más de stdout, como lo son Docker y el proveedor de alojamiento.

**Alternativas descartadas:**

| Alternativa | Por qué no |
|---|---|
| Sink OTLP de Serilog hacia el colector | Toca el backend: paquete nuevo, código, pruebas y el riesgo de una advertencia en un build que las trata como errores. Descartado por el responsable |
| Receptor de archivos del colector de OpenTelemetry | No conoce el nombre del contenedor, exige montar el directorio interno de Docker y mezcla la ingesta de registros con el presupuesto de memoria que el colector ya reparte entre métricas y trazas |
| Controlador de registros de Loki para Docker | Es un complemento que se instala en cada anfitrión, cambia la configuración de registros del contenedor de la API y, si el controlador falla, puede impedir que el contenedor arranque |
| Promtail | Grafana lo declaró completo en funciones y dirigió el desarrollo de la recolección a Alloy; empezar hoy con él es empezar con algo en retirada |

**Costo aceptado:** un contenedor más y el acceso al socket de Docker (D9).

## D3 — `TraceId` es metadato estructurado, no etiqueta

**Pregunta:** ¿cómo se guarda el identificador de traza en Loki para poder buscarlo?

**Decisión:** `TraceId` y `SpanId` se extraen del JSON y se guardan como *metadatos estructurados* del
registro. Las etiquetas indexadas son solo `servicio` y `nivel`.

**Razones:** cada combinación distinta de etiquetas crea un flujo nuevo en Loki. Un identificador por
petición como etiqueta crearía un flujo por petición, que es la forma conocida de degradar Loki hasta
dejarlo inservible. Es la misma regla que la spec 008 fijó para las métricas (§7.6 del documento):
*la métrica cuenta, el registro identifica*. Los metadatos estructurados se pueden filtrar sin
indexarse, que es lo que se necesita para buscar una petición concreta.

**Alternativa descartada:** no extraer nada y buscar el identificador como texto dentro de la línea.
Funciona para buscar, pero el enlace de registro a traza de Grafana se configura mucho más limpio
sobre un campo con nombre que sobre una expresión regular aplicada al texto.

## D4 — El nivel `Information` hay que suponerlo

**Pregunta:** ¿de dónde sale el nivel de cada registro?

**Decisión:** del campo `@l` del formato compacto de Serilog y, cuando falta, se asume `Information`.

**Razón:** el formato JSON compacto **omite** `@l` cuando el nivel es `Information`, para ahorrar
bytes en el nivel más frecuente. Sin esta regla, la mayoría de los registros aparecerían "sin nivel"
y el filtro por nivel —la primera consulta que alguien hace— devolvería resultados engañosos. La
tarea T002 lo confirma sobre una línea real antes de escribir la configuración.

## D5 — Tempo 3.0 en modo monolítico con almacenamiento local

**Pregunta:** ¿qué versión y qué modo de Tempo?

**Decisión:** la rama 3.0 (la *recomendada* según la política de versiones de Grafana; la 2.10 queda
solo en mantenimiento), en modo monolítico y con almacenamiento en disco local sobre un volumen
nombrado.

**Razones:** el modo monolítico de la 3.0 corre todos los componentes en un proceso y **no requiere
Kafka**, que solo exige el modo de microservicios. Es el modo que la documentación indica para
desarrollo y evaluación, y el volumen de este proyecto está órdenes de magnitud por debajo de su
límite.

**Advertencia que se hereda de la spec 008:** la 3.0 cambió la arquitectura interna y parte de la
configuración. Casi todas las guías que se encuentran buscando fueron escritas para la 2.x; copiar
una produce una configuración que puede no arrancar o, peor, arrancar ignorando claves. Es el mismo
fallo que la spec 008 documentó con los nombres de métricas del runtime (su D4). La configuración se
escribe contra la documentación de la 3.0 (T011).

## D6 — El receptor de Tempo no se publica en el anfitrión

**Pregunta:** ¿cómo recibe Tempo las trazas del colector?

**Decisión:** por OTLP dentro de la red `gsp-observabilidad`, sin publicar su puerto en el anfitrión.
Se publica solo su puerto HTTP de consulta (3200).

**Razón:** el colector ya publica 4317 y 4318 en el anfitrión, porque es por donde entra la
telemetría de la API cuando se ejecuta fuera de contenedor. Si Tempo publicara los mismos puertos,
el segundo contenedor no arrancaría. Si se publicaran en otros números, habría dos entradas de
trazas y alguien acabaría apuntando la API a la que se salta el colector. Una sola entrada es una
sola verdad.

## D7 — Retención de 7 días para registros y trazas

**Pregunta:** ¿cuánto se guardan?

**Decisión:** 7 días en ambos almacenes, con borrado automático.

**Razones:** cubre una semana de trabajo, que es el horizonte razonable para revisar un incidente en
un entorno local. Registros y trazas pesan mucho más que las series de Prometheus (que la spec 008
retiene 15 días), y el entorno corre en equipos personales. Con la misma retención en ambos almacenes,
un enlace de registro a traza nunca apunta a una traza ya borrada.

## D8 — Solo se recolecta la API

**Pregunta:** ¿se recolectan los registros de todos los contenedores?

**Decisión:** solo los del servicio `api`.

**Razones:** es lo que el punto 2 del caso de estudio pide trazar. Los registros de las herramientas
de monitoreo añadirían volumen y ruido al visor sin responder ninguna pregunta sobre la aplicación, y
se siguen pudiendo leer con `docker logs` cuando haga falta diagnosticar una herramienta. El filtro es
una regla de una línea: ampliarlo más adelante no requiere rediseño.

## D9 — El socket de Docker: riesgo aceptado y documentado

**Pregunta:** ¿qué implica que el recolector lea la API de Docker?

**Decisión:** se monta `/var/run/docker.sock` en el recolector, se documenta el riesgo en el README
del entorno y se limita su uso al entorno local y a la integración continua.

**Lo que hay que saber, dicho sin suavizar:** acceder al socket de Docker equivale a tener control
total del anfitrión. Montarlo en solo lectura **no** limita las operaciones que se pueden pedir a la
API de Docker; solo impide modificar el archivo del socket. La protección real es que el recolector
es una imagen oficial con versión fijada, que no publica ningún puerto de control y que este entorno
no se despliega en ningún servidor compartido.

**Alternativa considerada para después:** un proxy del socket que solo permita las operaciones de
lectura necesarias. Se descarta por ahora porque añade un contenedor más para proteger un entorno que
solo corre en el equipo de quien lo levanta; queda anotado para cuando el entorno se aloje fuera.

## D10 — La prueba que demuestra la correlación va de extremo a extremo

**Pregunta:** ¿cómo se sabe que la correlación funciona, y no solo que cada almacén recibe datos?

**Decisión:** la verificación toma un `TraceId` de un registro recién almacenado en Loki y lo pide a
Tempo. Si Tempo no lo tiene, la verificación falla.

**Razón:** comprobar cada almacén por separado deja pasar el fallo más probable: que ambos reciban
datos y los identificadores no coincidan (por ejemplo, un formato distinto o un campo mal extraído).
Un tablero aprovisionado sin errores con enlaces que no llevan a ninguna parte es el equivalente, en
este punto, al tablero permanentemente vacío que la spec 008 advierte.

## D11 — La frontera con la bitácora de auditoría

**Pregunta:** ¿quién construye el paso de una fila de `UserLog` a su traza?

**Decisión:** esta spec no toca la pantalla de auditoría. El tablero acepta un `TraceId` pegado a
mano (FR-019) y el quickstart documenta ese recorrido (FR-021).

**Razón:** `/usuarios/logs` es el visor del punto 3 (spec 010). El campo `TraceId` de `UserLog` ya
existe y es el puente; si el equipo del punto 3 decide añadir un enlace directo, el tablero de esta
spec ya ofrece el destino.

## Versiones consultadas

| Componente | Versión observada | Fuente | Nota |
|---|---|---|---|
| Loki | 3.7.8 (17 sep 2026) | Publicaciones del repositorio `grafana/loki` | Última de la rama 3.7 |
| Tempo | 3.0.x (3.0.3 o posterior) | Política de versiones recomendadas de Tempo y notas de publicación | La rama 3.0 es la recomendada; la 3.1 está en candidata |
| Alloy | 1.19.2 (26 ago 2026) | Publicaciones del repositorio `grafana/alloy` | — |
| Grafana | 13.0.2 | Ya fijada por la spec 008 | No cambia |
| Colector de OpenTelemetry | 0.161.0 | Ya fijada por la spec 008 | No cambia |

La etiqueta exacta de cada imagen se confirma en el registro de imágenes en T001, con el mismo
criterio que la spec 008: ninguna etiqueta móvil.
