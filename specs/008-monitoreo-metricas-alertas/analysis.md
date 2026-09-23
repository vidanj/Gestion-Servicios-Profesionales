# Análisis — Spec 008

> Reporte de consistencia entre [spec.md](spec.md), [plan.md](plan.md) y [tasks.md](tasks.md), y de
> cumplimiento de la [constitución](../../.specify/memory/constitution.md) v1.1.0.
> **Fecha:** 2026-09-23 · **Estado:** 1 hallazgo ALTO abierto, 3 medios, 2 bajos.

---

## 1. Cobertura de requisitos

Los 53 requisitos funcionales tienen al menos una tarea asignada. Resumen por grupo:

| Grupo | Requisitos | Tareas | Con prueba automatizada |
|---|---|---|---|
| US1 — Stack y métricas técnicas | FR-001 … FR-012 | T006–T018 | FR-002, FR-007, FR-008 (T017) |
| US2 — Niveles de servicio y alertas | FR-013 … FR-023 | T032–T041 | FR-022 (T039), FR-013 (T040) |
| US3 — Métricas de negocio | FR-024 … FR-033 | T042–T057 | Todos salvo FR-024, que es estructural |
| US4 — Despliegue | FR-034 … FR-043 | T019–T031 | Ninguno: se verifica por ejecución real |
| US5 — Guiones | FR-044 … FR-049 | T058–T063 | FR-045 (T063) |
| Transversales | FR-050 … FR-053 | T002, T004, T014, T049, T064, T065, T072 | FR-051 (T072) |

**Sin cobertura de prueba automatizada, por naturaleza:** los diez requisitos de US4. Un despliegue
no se puede probar en un entorno simulado sin dejar de ser un despliegue. Se compensan con T030 y
T031, que exigen **una ejecución real de cada camino**, incluida la reversión, con su tiempo medido.
Es una compensación consciente, no un olvido.

## 2. Hallazgos

### A1 — ALTO · La imagen desplegada no es la que se probó

**Dónde:** [contracts/despliegue.md](contracts/despliegue.md) frente a
[`docs/planeacion/estrategia-despliegue.md`](../../docs/planeacion/estrategia-despliegue.md) §2,
principio 1.

El documento de estrategia vigente establece: *«Imagen inmutable por commit; se despliega la misma
imagen que se probó»*. El mecanismo elegido aquí —el disparador de la plataforma de alojamiento—
hace que la plataforma **reconstruya** la imagen a partir del commit. Por tanto, la imagen que llega
a producción **no es el artefacto verificado en integración continua**, sino uno equivalente
construido en otro sitio.

**Por qué se acepta de todos modos:**

1. La plataforma actual construye su propia imagen y no ofrece desplegar una imagen externa sin
   reconfigurar el servicio, que es trabajo de la spec 003.
2. El `Dockerfile` es determinista salvo por las versiones que resuelva el gestor de paquetes del
   sistema base, y la construcción se verifica en cada cambio.
3. La verificación posterior al despliegue sondea el servicio **real ya desplegado**, de modo que
   una imagen distinta y defectuosa se detecta igualmente, aunque más tarde.

**Consecuencia que debe quedar escrita:** la trazabilidad es *commit → despliegue*, no
*artefacto → despliegue*. El documento entregable debe decirlo en el apartado del pipeline, y la
estrategia de despliegue debe reflejar que su principio 1 se cumplirá plenamente cuando la spec 003
introduzca el registro de imágenes.

**Acción:** ampliar T069 para corregir el principio 1 en lugar de solo actualizar su estado.
**Estado:** abierto, pendiente de la aprobación del responsable.

### A2 — MEDIO · "Despliegue automático" y "aprobación manual" conviven mal

**Dónde:** FR-009 de la [spec 003](../003-infraestructura-como-codigo/spec.md) frente a FR-043 de
esta spec.

Es el mismo conflicto que el análisis de la spec 003 registró como hallazgo I1 y que quedó abierto.
Si el ambiente exige revisor, el despliegue no es automático: queda en espera hasta que alguien
apruebe.

**Resolución propuesta:** no son incompatibles si se nombra bien lo que se promete. Aquí
«automático» significa **que nadie entra al panel de la plataforma ni copia una orden**, no que no
haya aprobación. Como el único ambiente desplegable hoy es producción, la aprobación es lo correcto;
lo que desaparece es el procedimiento manual, no el control.

**Acción:** el spec ya lo refleja en US4 («desplegar sin entrar al panel»), pero el criterio SC-008
mide «de integrar a servicio sano ≤ 20 min» y ese reloj **no debe incluir el tiempo de espera de la
aprobación humana**, o medirá la disponibilidad del revisor en lugar del pipeline.
**Estado:** abierto; afecta a cómo se mide SC-008, no a qué se construye.

### A3 — MEDIO · El sondeo del servicio publicado puede alertar antes de estar configurado

**Dónde:** FR-009 (T009, T010) frente a T020.

El grupo de sondeo del servicio publicado necesita una dirección real. Si se escribe un marcador y
se despliega el entorno antes de T020, el sondeo fallará de forma permanente y disparará la alerta
crítica de indisponibilidad con la etiqueta de producción. La primera alerta que vea cualquiera será
falsa, que es la peor forma posible de estrenar un sistema de alertas.

**Acción:** la dirección se toma de una variable, y el grupo de sondeo queda **inactivo** mientras
esa variable no esté definida. Debe anotarse explícitamente en T009 al ejecutarla.
**Estado:** abierto, con mitigación definida.

### A4 — MEDIO · El documento entregable depende de datos que se producen al final

**Dónde:** T064 (Fase 6) depende de los tiempos reales que producen T030 y T031 (Fase 2).

El orden de fases lo respeta, pero el documento no puede redactarse por completo antes de haber
desplegado y revertido de verdad. Es la razón de que el generador separe los datos de la maquetación:
lo que falte se imprime como pendiente y resaltado, en lugar de inventarse.

**Acción:** ninguna en el diseño; queda advertido para que no se cierre el documento antes de tiempo.
**Estado:** aceptado.

### A5 — BAJO · La alerta de base de datos depende de una etiqueta compartida

**Dónde:** FR-016, T033.

La regla que distingue «el proceso vive pero la base no responde» compara las dos sondas por su
ambiente. Si ambas no llevan exactamente la misma etiqueta, la comparación no encuentra pareja y la
alerta **nunca dispara**, sin dar ningún error.

**Acción:** T039 debe incluir un caso de prueba para esta regla concreta; es de las que fallan en
silencio.
**Estado:** abierto, con acción asignada.

### A6 — BAJO · El nombre del método instrumentado difiere del previsto

**Dónde:** planeación inicial frente al código real.

La planeación mencionaba `UpdateRequestStatusAsync`; el método real es `UpdateStatusAsync`. Ya
corregido en [data-model.md](data-model.md) y en T047. Se registra porque es el tipo de detalle que,
copiado entre documentos sin verificar, produce una tarea que no se puede ejecutar.
**Estado:** resuelto.

## 3. Cumplimiento de la constitución

| Principio | Evaluación |
|---|---|
| I — Plan aprobado antes del código | Cumple. Los artefactos se entregan en un PR propio, previo al de implementación |
| II — Capas inviolables | Cumple. T042–T048 emiten solo desde servicios; T044 registra en el punto único de composición |
| III — Contratos por DTO | No aplica: no se añaden ni modifican endpoints |
| IV — Esquema solo por migración | No aplica: no hay cambio de esquema |
| V — Pruebas obligatorias y honestas | Cumple con la salvedad documentada de US4. Ninguna prueba existente se borra ni se salta |
| VI — Calidad sin silenciadores | Cumple. Es la razón de descartar los dos paquetes en preestreno (research D1) y de T072 |
| VII — Stack del frontend cerrado | No aplica: no se toca el frontend |
| Seguridad y Datos | Cumple. FR-026 y FR-049 lo hacen requisito verificable, no recomendación |
| Trazabilidad SDD | Cumple. Todas las tareas llevan historia y requisitos |

**Hallazgos CRITICAL:** ninguno. La constitución no queda violada por ninguno de los seis hallazgos.

## 4. Antes de implementar

1. Resolver **A1**: el responsable debe aceptar por escrito que la trazabilidad es por commit y no
   por artefacto, o posponer US4 hasta que exista el registro de imágenes de la spec 003.
2. Resolver **A2**: decidir si el reloj de SC-008 se detiene mientras espera aprobación.
3. Confirmar las mitigaciones de **A3** y **A5** al ejecutar T009 y T039.
