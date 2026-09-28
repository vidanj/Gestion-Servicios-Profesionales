# Evidencia de la prueba de carga — GET /api/Auth/me
**Integrante:** Federico Antonio Gutiérrez Amaya (@faga)
**Script:** `tests/load/fagaprueba.js`
**Resumen exportado:** `tests/load/resultados/faga-resumen.json`
**Captura:** `tests/load/resultados/faga-captura.png`

## Resultado

| Métrica | Valor | Umbral | Resultado |
|---|---|---|---|
| Percentil 95 | 10.82 ms | < 5000 ms | Cumple |
| Peticiones totales | 464 | — | — |
| Tasa de fallos (`http_req_failed`) | 0.00% | — | — |
| Verificaciones (`checks_succeeded`) | 100% (1853/1853) | — | — |
| Usuarios virtuales | 5 | ≥ 5 | Cumple |

## Análisis

El endpoint `GET /api/Auth/me` arroja un percentil 95 de 10.82 ms frente a un umbral acordado de
5000 ms, un margen de casi 500 veces. Esto es consistente con lo que describe el documento de
planeación (sección 4.2 / 6.3): este endpoint solo valida el token contra la firma JWT y no requiere
una consulta compleja a la base de datos, por lo que su costo es mínimo comparado con el de
`POST /api/Auth/login`.

La dispersión es baja: el máximo registrado (228.04 ms) es un valor aislado, probablemente asociado
al arranque de la primera conexión, mientras que la mediana (7.04 ms) y el promedio (7.82 ms) están
muy cerca entre sí — el sistema no muestra señales de degradación bajo esta concurrencia.

La verificación de "no fue rechazado por autorización" y "no fue rechazado por rate limit" están al
100%, lo que confirma que las 464 peticiones ejercitaron el flujo real de validación de token, y no
una respuesta de rechazo temprano que falsamente diera buenos tiempos (el mismo riesgo que se
documentó en la sección 7 del documento para el endpoint de login).

Al igual que con el resultado de login, el umbral de 5000 ms no discrimina a este nivel de carga
(5 usuarios virtuales): encontrar el punto de quiebre del sistema requeriría una prueba de estrés
con muchos más usuarios, lo cual queda fuera del alcance de este ejercicio.