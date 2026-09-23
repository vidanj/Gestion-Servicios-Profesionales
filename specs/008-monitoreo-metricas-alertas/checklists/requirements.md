# Specification Quality Checklist: Monitoreo de la aplicación

**Purpose**: Validar la calidad del [spec.md](../spec.md) antes de planear e implementar.
**Created**: 2026-09-23
**Feature**: [spec.md](../spec.md)

> **Propiedad de la revisión:** la marca la pone quien revisa el spec, no quien lo escribe.
> **Semántica de las marcas:** `[x]` significa *criterio de calidad revisado y satisfecho*, no
> *implementación terminada*.

## Calidad del contenido

- [ ] El spec describe el **qué** y el **porqué**; ninguna decisión de implementación se cuela en él
- [ ] Está escrito para quien toma la decisión, no solo para quien programa
- [ ] Toda sección obligatoria está presente
- [ ] No quedan marcadores de pendiente ni preguntas sin resolver
- [ ] Las clarificaciones registran la pregunta, la resolución **y la razón**

## Completitud de los requisitos

- [ ] Cada requisito funcional es verificable: se puede decir si se cumple o no sin discutir
- [ ] Ningún requisito usa palabras que no se pueden medir ("rápido", "robusto", "amigable")
- [ ] Cada criterio de éxito indica **cómo se mide**
- [ ] Los criterios de éxito son independientes de la herramienta elegida
- [ ] Cada historia declara cómo se prueba por separado
- [ ] Los escenarios cubren el camino feliz **y** los bordes
- [ ] Los supuestos están escritos, en particular los que dependen de terceros
- [ ] Lo que queda fuera de alcance está enumerado, con su razón

## Preparación de la feature

- [ ] Cada historia tiene al menos un requisito funcional que la respalda
- [ ] Cada requisito funcional pertenece a alguna historia o está marcado como transversal
- [ ] Las prioridades reflejan dependencias reales, no preferencias
- [ ] Se identifica qué historias son independientes entre sí
- [ ] Hay un mínimo viable identificable

## Específico de esta spec

- [ ] Toda métrica propuesta responde a una pregunta enunciada; ninguna está "porque se puede medir"
- [ ] Toda alerta se deriva de un nivel de servicio declarado
- [ ] Los niveles de servicio son defendibles para la plataforma realmente contratada, no para una ideal
- [ ] La prohibición de datos personales en las etiquetas es un requisito, no una recomendación
- [ ] El alcance del punto 1 está separado sin ambigüedad de los puntos 2 y 3
- [ ] La decisión de no exponer un endpoint de métricas está justificada y es verificable
