# Spec 008 — Monitoreo de la aplicación: métricas, niveles de servicio, alertas y despliegue verificado

> **Estado:** planeación (sin implementar) · **Fecha:** 2026-09-23 · **Rama:** `ci/251-monitoreo-y-despliegue`
>
> **Origen:** caso de estudio de liberación y despliegue continuo, punto 1 (métricas para el
> monitoreo). Absorbe el issue [#189](https://github.com/vidanj/Gestion-Servicios-Profesionales/issues/189)
> y reemplaza el enfoque del issue [#187](https://github.com/vidanj/Gestion-Servicios-Profesionales/issues/187).
> Implementa la US3 de la [spec 003](../003-infraestructura-como-codigo/spec.md).
>
> **Relacionados:** [plan](plan.md) · [research](research.md) · [modelo de datos](data-model.md) ·
> [contrato de la métrica](contracts/metricas.md) · [contrato del despliegue](contracts/despliegue.md) ·
> [quickstart](quickstart.md) · [tareas](tasks.md) · [análisis](analysis.md) ·
> [constitución](../../.specify/memory/constitution.md) ·
> [estrategia de despliegue](../../docs/planeacion/estrategia-despliegue.md)

## 1. Contexto y valor

El sistema ya produce telemetría y no la consume nadie.
[`TelemetryConfiguration.cs`](../../backend/SistemaServicios.API/Extensions/TelemetryConfiguration.cs)
registra la instrumentación de métricas de ASP.NET Core y un exportador OTLP **condicional** a
`OTEL_EXPORTER_OTLP_ENDPOINT`; esa variable está vacía y no hay colector desplegado. El resultado es
que hoy la única forma de saber si la aplicación está sana es abrirla en el navegador.

Al mismo tiempo, el despliegue se lanza a mano desde el panel de Render, la versión desplegada no se
liga a un commit y no existe procedimiento de reversión (#189). Los objetivos DORA declarados en
[`estrategia-despliegue.md`](../../docs/planeacion/estrategia-despliegue.md) §8 no tienen ninguna
fuente de medición: dicen "desconocido" y "no se mide".

Esta spec cierra las dos brechas a la vez, porque son la misma pregunta vista desde dos lados: *¿cómo
sé que la versión que acabo de liberar funciona?* Un despliegue sin verificación es una apuesta, y un
monitoreo sin despliegue automatizado solo sirve para enterarse tarde.

**Valor entregado:** un tablero que responde "¿está sana la aplicación?" en un vistazo, alertas que
avisan sin que nadie esté mirando, métricas de negocio que distinguen "la API responde 200" de "el
negocio funciona", y un despliegue que se verifica solo y se revierte en minutos.

**Decisión estructural (detalle en [research](research.md), D1):** las métricas **no** se exponen en
un endpoint `/metrics` de la API. La aplicación empuja OTLP a un OpenTelemetry Collector, y es el
colector quien publica en formato Prometheus. Así se evita introducir paquetes en preestreno en un
build que trata las advertencias como errores, y se evita abrir superficie de información en una API
pública.

## 2. Historias de usuario

### US1 — Ver el estado de la aplicación en un tablero (P1, MVP)

**Como** responsable de la operación, **quiero** un tablero que muestre disponibilidad, latencia,
errores y saturación, **para** saber si el sistema está sano sin leer registros línea por línea.

**Por qué esta prioridad:** es el cimiento. Sin recolección no hay alertas (US2), ni métricas de
negocio visibles (US3), ni verificación con datos (US4). Además entrega valor por sí sola: la
instrumentación automática ya existe, solo le falta un consumidor.

**Prueba independiente:** levantar el stack, generar tráfico y comprobar que el tablero muestra
p95, tasa de error y volumen sin haber tocado la lógica de negocio.

```
DADO el stack de monitoreo y la aplicación en marcha
CUANDO se generan peticiones correctas y erróneas durante un minuto
ENTONCES el tablero muestra volumen, latencia p95, proporción de 5xx y saturación
Y ninguna de esas cifras exige configuración manual posterior al arranque
```

```
DADO que la aplicación se ejecuta sin OTEL_EXPORTER_OTLP_ENDPOINT
CUANDO arranca
ENTONCES lo hace con normalidad y no intenta exportar a ningún destino
```

### US2 — Enterarme de un problema sin estar mirando (P2)

**Como** responsable de la operación, **quiero** alertas que se disparen cuando se incumple un nivel
de servicio acordado, **para** reaccionar antes de que el problema me lo reporte un usuario.

**Por qué esta prioridad:** un tablero solo sirve mientras alguien lo mira. Va después de US1 porque
necesita las series que US1 produce.

**Prueba independiente:** detener la aplicación, esperar y comprobar que la alerta crítica aparece
en el gestor; restablecerla y comprobar que se resuelve sola.

```
DADO el stack en marcha y la aplicación sana
CUANDO la aplicación deja de responder
ENTONCES en 3 minutos o menos existe una alerta crítica con su descripción y su acción sugerida
Y al restablecer la aplicación la alerta se marca como resuelta en 2 minutos o menos
```

```
DADO un servicio que despierta de una siesta del plan gratuito y tarda 60 segundos
CUANDO se sondea durante ese arranque en frío
ENTONCES no se dispara ninguna alerta crítica
```

```
DADO un periodo sin tráfico en el que una sola petición devuelve error de servidor
CUANDO se evalúan las reglas
ENTONCES no se dispara la alerta de tasa de error, porque el volumen no es significativo
```

### US3 — Medir el negocio, no solo la infraestructura (P2)

**Como** responsable del producto, **quiero** contar solicitudes, inicios de sesión y respaldos,
**para** distinguir "la API responde 200" de "el negocio funciona".

**Por qué esta prioridad:** es lo que convierte el monitoreo técnico en información de producto. Un
fallo que deja de crear solicitudes pero sigue devolviendo 200 es invisible para US1.

**Prueba independiente:** ejecutar un inicio de sesión fallido y una solicitud de servicio, y
comprobar que los contadores correspondientes se incrementan con las etiquetas esperadas.

```
DADO un intento de inicio de sesión con credenciales incorrectas
CUANDO se procesa
ENTONCES se incrementa el contador de intentos con resultado "credenciales_invalidas"
Y la métrica no contiene el correo, ni el identificador, ni ningún otro dato personal
```

```
DADO un cambio de estado de solicitud que el servidor rechaza por ser una transición inválida
CUANDO se procesa
ENTONCES se cuenta con resultado "rechazada" y con los estados de origen y destino
```

```
DADO que el registro de una métrica falla por cualquier motivo
CUANDO ocurre durante un inicio de sesión
ENTONCES el inicio de sesión termina con su resultado normal, sin propagar el fallo
```

### US4 — Desplegar sin entrar al panel y poder volver atrás (P1)

**Como** responsable de la operación, **quiero** que integrar en `dev` despliegue y verifique solo, y
poder revertir a una versión sana en minutos, **para** dejar de depender de un procedimiento manual
que nadie puede auditar.

**Por qué esta prioridad:** es el issue #189, abierto desde hace meses, y la mitad pendiente de #131.
**No depende de US1–US3:** puede implementarse en paralelo.

**Prueba independiente:** integrar un cambio trivial en `dev` y observar el despliegue verificado;
después ejecutar una reversión al commit anterior y medir cuánto tarda.

```
DADO un commit integrado en dev con los cuatro workflows de CI en verde
CUANDO termina la integración continua
ENTONCES se despliega ese commit y el despliegue solo se da por bueno cuando la sonda de
        preparación responde sana de forma sostenida
```

```
DADO un commit cuyas pruebas están en rojo
CUANDO se intenta desplegar
ENTONCES el despliegue se detiene antes de disparar nada, nombrando la comprobación que falló
```

```
DADO un despliegue que nunca llega a responder sano
CUANDO se agota el plazo
ENTONCES el resultado es un fallo visible con diagnóstico, no un éxito silencioso
```

```
DADO una versión defectuosa en marcha
CUANDO se ejecuta la reversión indicando el commit sano anterior
ENTONCES el servicio vuelve a estar sano y queda registrado cuánto tardó
```

```
DADO dos despliegues solicitados al mismo tiempo
CUANDO coinciden
ENTONCES se ejecutan uno después del otro, sin cancelarse entre sí
```

### US5 — Reproducir el entorno con una orden (P3)

**Como** integrante del equipo, **quiero** levantar, poblar y verificar el entorno de monitoreo con
guiones versionados, **para** que la comprobación sea la misma en mi equipo y en la integración
continua.

**Por qué esta prioridad:** es comodidad y evidencia, no capacidad nueva. Va al final.

**Prueba independiente:** en un equipo limpio, una sola orden deja el entorno en marcha y otra
responde si el monitoreo está completo.

```
DADO un equipo con Docker y el repositorio recién clonado
CUANDO se ejecuta el guion de arranque del monitoreo
ENTONCES el stack queda en marcha y se imprimen las direcciones de cada herramienta
```

```
DADO el entorno en marcha y tráfico generado
CUANDO se ejecuta el guion de verificación
ENTONCES informa qué métricas del catálogo faltan, si falta alguna, y termina con código distinto
        de cero
```

## 3. Requisitos funcionales

### US1 — Stack de monitoreo y métricas técnicas

| ID | Requisito |
|---|---|
| FR-001 | La API **DEBE** exportar sus métricas por OTLP y **NO DEBE** exponer un endpoint propio de métricas. |
| FR-002 | La exportación **DEBE** seguir siendo condicional: sin endpoint configurado, la aplicación arranca con normalidad y no exporta. |
| FR-003 | **DEBE** existir un colector que reciba OTLP y publique las métricas en formato Prometheus. |
| FR-004 | Las métricas **DEBEN** recogerse con una periodicidad no mayor a 15 segundos. |
| FR-005 | El tablero y su origen de datos **DEBEN** aprovisionarse de forma declarativa y versionada, sin configuración manual tras el arranque. |
| FR-006 | El tablero técnico **DEBE** mostrar, como mínimo: disponibilidad, volumen de peticiones, latencia p95, proporción de respuestas 5xx y saturación. |
| FR-007 | **DEBE** incluirse la instrumentación del tiempo de ejecución de .NET (memoria, recolector de basura, grupo de hilos y excepciones). |
| FR-008 | **DEBEN** recogerse las métricas del limitador de peticiones, que no viajan con la instrumentación general de ASP.NET Core. |
| FR-009 | **DEBE** existir un sondeo externo de las sondas de vida y de preparación, tanto del entorno local como del servicio publicado. |
| FR-010 | Todas las imágenes de contenedor **DEBEN** fijarse a una versión exacta; **NO DEBEN** usarse etiquetas móviles como `latest`. |
| FR-011 | Los datos de Prometheus, Alertmanager y Grafana **DEBEN** persistir en volúmenes nombrados. |
| FR-012 | El stack de monitoreo **NO DEBE** modificar el archivo de composición de la aplicación, y **DEBE** poder ejecutarse con la aplicación detenida. |

### US2 — Niveles de servicio y alertas

| ID | Requisito |
|---|---|
| FR-013 | Las reglas de alerta **DEBEN** estar versionadas y validarse automáticamente en cada cambio. |
| FR-014 | Cada alerta **DEBE** declarar severidad, tiempo de espera y anotaciones en español que incluyan qué ocurre, por qué ese umbral y qué hacer. |
| FR-015 | **DEBE** existir una alerta de indisponibilidad del servicio. |
| FR-016 | **DEBE** existir una alerta que distinga "el proceso vive pero la base de datos no responde". |
| FR-017 | **DEBE** existir una alerta de proporción de errores de servidor, que **NO DEBE** dispararse con volumen de tráfico insignificante. |
| FR-018 | **DEBEN** existir alertas de latencia en dos niveles: el objetivo interno y el nivel de servicio acordado. |
| FR-019 | **DEBE** existir una alerta de saturación de memoria y otra de ausencia de métricas. |
| FR-020 | Los tiempos de espera de las alertas **DEBEN** absorber el arranque en frío del plan de alojamiento, de modo que una siesta no se reporte como caída. |
| FR-021 | El gestor de alertas **DEBE** agrupar por alerta y ambiente, e inhibir las alertas derivadas cuando ya está disparada la causa raíz. |
| FR-022 | Las reglas **DEBEN** tener casos de prueba automatizados que comprueben que disparan cuando deben y **no** cuando no deben. |
| FR-023 | Las expresiones compartidas entre tablero y alertas **DEBEN** definirse una sola vez como reglas de registro. |

### US3 — Métricas de negocio

| ID | Requisito |
|---|---|
| FR-024 | Las métricas de negocio **DEBEN** emitirse desde la capa de servicios; **NO DEBEN** emitirse desde controladores ni repositorios. |
| FR-025 | El medidor **DEBE** consumirse por inyección de dependencias a través de un contrato en `Interfaces/`. |
| FR-026 | Ninguna etiqueta **DEBE** contener datos personales ni identificadores: correo, identificador de usuario, de solicitud o de servicio. |
| FR-027 | Todas las etiquetas **DEBEN** tener un dominio de valores cerrado y conocido de antemano. |
| FR-028 | **DEBE** contarse la creación de solicitudes de servicio, distinguiendo el resultado. |
| FR-029 | **DEBEN** contarse los cambios de estado de solicitud, con estado de origen, de destino y si fueron aceptados o rechazados. |
| FR-030 | **DEBEN** contarse los intentos de autenticación, distinguiendo éxito, credenciales inválidas y cuenta inactiva. |
| FR-031 | **DEBEN** contarse las altas de usuario, distinguiendo el resultado. |
| FR-032 | **DEBEN** contarse los respaldos por resultado y medirse su duración. |
| FR-033 | El registro de una métrica **NO DEBE** alterar el resultado de la operación ni propagar excepciones. |

### US4 — Despliegue verificado y reversible

| ID | Requisito |
|---|---|
| FR-034 | El despliegue **DEBE** dispararse mediante un secreto que **NO DEBE** aparecer en los registros de ejecución. |
| FR-035 | Solo **DEBE** desplegarse un commit cuyas comprobaciones de integración continua estén en verde **para ese mismo commit**. |
| FR-036 | Los despliegues al mismo ambiente **DEBEN** serializarse, sin cancelar el que esté en curso. |
| FR-037 | Cada despliegue **DEBE** verificarse contra la sonda de preparación, y **DEBE** exigir varias respuestas sanas consecutivas antes de darlo por bueno. |
| FR-038 | El plazo de verificación **DEBE** contemplar el arranque en frío y la construcción de la imagen en la plataforma. |
| FR-039 | Un despliegue que no queda sano dentro del plazo **DEBE** marcarse como fallido de forma visible, con diagnóstico y sin exponer secretos. |
| FR-040 | **DEBE** poder ejecutarse manualmente indicando un commit concreto. |
| FR-041 | La reversión **DEBE** verificarse igual que un despliegue, y **DEBE** ejecutarse al menos una vez de verdad, registrando cuánto tardó. |
| FR-042 | El resultado **DEBE** advertir de que una migración destructiva no se revierte redesplegando la versión anterior. |
| FR-043 | El despliegue **DEBE** requerir aprobación en un ambiente protegido con revisor. |

### US5 — Guiones del entorno de liberación

| ID | Requisito |
|---|---|
| FR-044 | **DEBE** existir un guion que levante el entorno de monitoreo y espere a que cada herramienta responda sana. |
| FR-045 | **DEBE** existir un guion que verifique el catálogo completo de métricas y las reglas cargadas. |
| FR-046 | **DEBE** existir un guion que genere tráfico representativo para poblar el entorno. |
| FR-047 | **DEBE** existir un guion que despliegue y verifique, equivalente al flujo automatizado. |
| FR-048 | Cada guion **DEBE** ofrecerse en las dos variantes del proyecto (PowerShell y shell) y **DEBE** terminar con código distinto de cero cuando falla. |
| FR-049 | Ningún guion **DEBE** contener credenciales: todas las variables se leen del entorno. |

### Transversales

| ID | Requisito |
|---|---|
| FR-050 | **NO DEBE** añadirse ningún paquete en preestreno. |
| FR-051 | La compilación **DEBE** terminar sin advertencias y sin ningún silenciador de análisis. |
| FR-052 | Toda variable de entorno nueva **DEBE** documentarse en `.env.example`, sin valores reales. |
| FR-053 | El documento entregable **DEBE** reservar de forma explícita un apartado para el visor de trazabilidad y otro para el visor de auditoría, indicando qué existe ya y qué falta, sin desarrollarlos. |

## 4. Criterios de éxito

| ID | Criterio | Cómo se mide |
|---|---|---|
| SC-001 | Con el entorno en marcha, todas las métricas del catálogo son consultables | Guion de verificación, en menos de 60 s |
| SC-002 | El tablero se abre con paneles poblados sin configuración manual posterior al arranque | Validación visual del responsable |
| SC-003 | Las reglas de alerta superan la validación y sus casos de prueba con cero fallos | Job de validación del workflow |
| SC-004 | Detener la aplicación genera la alerta crítica en ≤ 3 min, y restablecerla la resuelve en ≤ 2 min | Prueba manual guiada por el quickstart |
| SC-005 | Un arranque en frío de 60 s no dispara ninguna alerta crítica | Casos de prueba de las reglas |
| SC-006 | Ninguna serie de negocio contiene etiquetas con datos personales | Prueba unitaria + revisión del diff |
| SC-007 | La compilación en Release termina con 0 advertencias y 0 silenciadores | Workflow de formato y análisis |
| SC-008 | De integrar en `dev` a servicio sano verificado transcurren ≤ 20 min | Duración del workflow de despliegue |
| SC-009 | Una reversión deja el servicio sano en ≤ 10 min, medida al menos una vez | Ejecución real registrada |
| SC-010 | La cardinalidad del trabajo de la API se mantiene por debajo de 2 000 series tras una hora de tráfico | Consulta de conteo de series |
| SC-011 | El documento entregable contiene los cinco apartados que exige el caso de estudio y los dos apartados reservados | Revisión del responsable |

## 5. Clarificaciones resueltas

| # | Pregunta | Resolución | Razón |
|---|---|---|---|
| C1 | ¿Cómo se exponen las métricas a Prometheus? | Mediante un OpenTelemetry Collector; la API solo empuja OTLP | Evita los dos paquetes en preestreno que bloquearon #187, y no abre superficie de información en una API pública |
| C2 | ¿El stack va en el archivo de composición existente o en uno propio? | Uno propio, en `monitoring/` | El monitoreo debe seguir en pie cuando la aplicación se cae; y el compose raíz existe para verificar cabeceras reenviadas, no para levantar siete servicios |
| C3 | ¿Cómo se observa el servicio publicado, si su contenedor no puede alcanzar un colector local? | Con un sondeo externo de las sondas de salud | Es lo único que puede observar Render desde fuera; da disponibilidad y tiempo de arranque en frío reales |
| C4 | ¿El arranque en frío cuenta como indisponibilidad? | Se mide y se declara, pero **no** consume presupuesto de error | Es una característica conocida y aceptada del plan contratado, no un defecto del software |
| C5 | ¿Qué umbral de latencia se acuerda? | 1,5 s como objetivo interno y 5 s como acuerdo formal | El techo de 5 s ya estaba acordado en el plan de pruebas de carga; contradecirlo crearía dos verdades |
| C6 | ¿Se implementa también el despliegue, o solo el monitoreo? | También el despliegue | El caso de estudio exige vincular la herramienta de liberación continua con el entorno de despliegue; y sin despliegue verificado no hay forma de medir los objetivos DORA |
| C7 | ¿Qué pasa con el issue #187, que pedía el exportador en proceso? | Se cierra con un comentario que enlaza la decisión | Dejarlo abierto crearía una contradicción entre el código y el registro de intenciones del proyecto |

## 6. Supuestos

- Existe una persona con acceso de administración a Render capaz de obtener el deploy hook y de
  crear el ambiente protegido en GitHub. Sin eso, US4 no puede completarse.
- El parámetro que permite indicar un commit concreto al disparar el hook se comporta como está
  documentado; se comprueba a mano antes de escribir el workflow. Si no fuera así, la alternativa es
  la interfaz de programación de la plataforma, cuyo secreto ya está contemplado.
- El stack de monitoreo se ejecuta en el equipo de quien lo levanta, no en la nube: es un piloto
  demostrable, no una plataforma de observación permanente.
- El paquete de instrumentación del tiempo de ejecución de .NET está disponible en versión estable.
  Si introdujera una advertencia, se retira la tarea antes que silenciarla.
- El equipo no levanta a la vez el stack de análisis estático y el de monitoreo.

## 7. Fuera de alcance

| Tema | Por qué queda fuera |
|---|---|
| Visor de trazabilidad (logs y trazas) | Es el punto 2 del caso de estudio; aquí solo se deja preparada la tubería de trazas del colector y el apartado reservado en el documento |
| Visor de auditoría | Es el punto 3 del caso de estudio |
| Endpoint `/metrics` en la API | Decisión D1: exigiría un paquete en preestreno y abriría superficie de información |
| Instrumentación de Entity Framework Core | Sigue en preestreno; las trazas a nivel de controlador de base de datos ya cubren la pregunta de dónde se fue el tiempo |
| Almacenamiento de métricas de larga duración y alta disponibilidad del propio monitoreo | Un piloto local no lo justifica |
| Envío de alertas por correo o mensajería | Se deja configurado y desactivado: un entorno de pruebas que envía correos en cada ensayo se vuelve inusable |
| Infraestructura declarativa con Terraform | Es la spec 003; esta spec no la presupone ni la bloquea |
| Métricas del frontend | El caso de estudio se centra en la aplicación de servidor |

## 8. Dependencias

| Depende de | Estado |
|---|---|
| Instrumentación de OpenTelemetry ya registrada en la API | **Hecho** (issue #124, PR #186) |
| Sondas de vida y preparación | **Hecho** (issue #123) |
| Imagen de contenedor verificada en integración continua | **Hecho** (issue #131 primera mitad, PR #188) |
| Deploy hook de Render y ambiente protegido en GitHub | **Pendiente**, requiere acción del responsable |
| Issue #189 (disparador, verificación y reversión) | Se resuelve con esta spec |
| Issue #187 (exportador Prometheus en proceso) | Se cierra por cambio de enfoque |
