# Specification Quality Checklist: Visor de trazabilidad

**Purpose**: Validar la calidad del [spec.md](../spec.md) antes de planear e implementar.
**Created**: 2026-09-24
**Feature**: [spec.md](../spec.md)

> **Propiedad de la revisión:** la marca la pone quien revisa el spec, no quien lo escribe.
> **Semántica de las marcas:** `[x]` significa *criterio de calidad revisado y satisfecho*, no
> *implementación terminada*.

## Calidad del contenido

- [ ] El spec describe el **qué** y el **porqué**; las decisiones de herramienta viven en research y plan
- [ ] Está escrito para quien toma la decisión, no solo para quien configura
- [ ] Toda sección obligatoria está presente
- [ ] No quedan marcadores de pendiente ni preguntas sin resolver
- [ ] Las clarificaciones registran la pregunta, la resolución **y la razón**

## Completitud de los requisitos

- [ ] Cada requisito funcional es verificable: se puede decir si se cumple o no sin discutir
- [ ] Ningún requisito usa palabras que no se pueden medir ("rápido", "robusto", "amigable")
- [ ] Cada criterio de éxito indica **cómo se mide**
- [ ] Cada historia declara cómo se prueba por separado
- [ ] Los escenarios cubren el camino feliz **y** los bordes
- [ ] Los supuestos están escritos, en particular los que dependen de la spec 008
- [ ] Lo que queda fuera de alcance está enumerado, con su razón

## Preparación de la feature

- [ ] Cada historia tiene al menos un requisito funcional que la respalda
- [ ] Cada requisito funcional pertenece a alguna historia o está marcado como transversal
- [ ] Las prioridades reflejan dependencias reales, no preferencias
- [ ] Se identifica qué historias son independientes entre sí (US1 y US2)
- [ ] Hay un mínimo viable identificable

## Específico de esta spec

- [ ] Ningún identificador de valores ilimitados (`TraceId`, rutas, IP, correo) es etiqueta del almacén de registros
- [ ] La correlación se prueba de extremo a extremo, no almacén por almacén
- [ ] La frontera con el punto 3 (spec 010) está escrita y no deja trabajo sin dueño
- [ ] Los límites heredados de la spec 008 (servicio publicado) se declaran, no se omiten
- [ ] El compromiso de no tocar el backend es verificable (SC-006)
