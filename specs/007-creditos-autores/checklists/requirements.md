# Checklist de calidad de requisitos — Spec 007

> Estas son las "pruebas unitarias del enunciado": no verifican el sistema, verifican que el spec
> esté bien escrito. El revisor marca cada casilla antes de aprobar el plan.

## Claridad

- [ ] Cada requisito funcional describe **comportamiento observable**, no implementación
- [ ] Ningún requisito menciona nombres de clases, tablas o bibliotecas
- [ ] Cada requisito usa una formulación imperativa inequívoca
- [ ] No quedan marcas de ambigüedad sin resolver
- [ ] Los términos del dominio se usan de forma consistente en todo el documento

## Completitud

- [ ] Cada historia de usuario tiene al menos un escenario de aceptación
- [ ] Cada historia tiene al menos un escenario de camino no feliz
- [ ] Los casos límite están cubiertos: lista vacía, campo opcional ausente, operación repetida
- [ ] Se declara explícitamente qué queda fuera de alcance y por qué
- [ ] Los supuestos están escritos, no implícitos

## Verificabilidad

- [ ] Cada criterio de éxito es medible, con un umbral o una condición binaria
- [ ] Ningún criterio de éxito usa adjetivos sin métrica
- [ ] Cada requisito funcional puede convertirse en al menos una prueba automatizada
- [ ] Existe trazabilidad de cada requisito a una tarea

## Consistencia

- [ ] Ningún requisito contradice a otro
- [ ] Los requisitos no contradicen la constitución del proyecto
- [ ] Las decisiones del plan no contradicen lo que el spec declara fuera de alcance
- [ ] El contrato expone exactamente los recursos que los requisitos implican, ni más ni menos

## Independencia de las historias

- [ ] La historia de prioridad más alta es entregable por sí sola
- [ ] Las historias posteriores no son requisito de la primera
- [ ] Se puede detener el trabajo al terminar cualquier historia y lo entregado tiene valor
