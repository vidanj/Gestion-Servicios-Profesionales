# Generadores de documentos

Producen la versión en formato Word de los documentos del proyecto, a partir de su fuente en
markdown. **La fuente en markdown es la que manda**; el `.docx` es un derivado que se regenera
cuantas veces haga falta.

## Qué hay aquí

| Archivo | Qué es |
|---|---|
| `generar_monitoreo.py` | Genera el documento del caso de estudio de monitoreo |
| `enlaces-monitoreo.json` | Issues, PR, ramas, ejecuciones y mediciones de ese documento |

Fuente del contenido: [`docs/monitoreo-metricas-y-alertas.md`](../monitoreo-metricas-y-alertas.md).

## Requisitos

```powershell
pip install python-docx
```

## Cómo se ejecuta

Desde la raíz del repositorio:

```powershell
python docs/reporte/generar_monitoreo.py
```

Imprime dónde quedó el archivo y cuántos datos siguen pendientes.

## Dónde cae la salida

En `.github/DRAFTS/`, que **git ignora**. Es deliberado: se versionan la fuente y el generador, no
el binario. Así el documento siempre se puede reproducir y no se llena el historial de versiones de
un `.docx` cuyo diff nadie puede leer.

## Cómo se completan las evidencias

Los datos vacíos en `enlaces-monitoreo.json` se imprimen como `[PENDIENTE]` **resaltados en
amarillo**. Nunca se inventan: una evidencia que todavía no existe tiene que verse que no existe.

1. Abrir `enlaces-monitoreo.json`.
2. Rellenar lo que ya exista: número y enlace del PR, commit, enlaces de las ejecuciones y las
   mediciones tomadas.
3. Volver a ejecutar el generador.

## Después de generar

1. Abrir el documento en Word.
2. **Actualizar el índice**: clic derecho sobre él → *Actualizar campo* → *Actualizar toda la
   tabla*. El documento pide la actualización al abrirse, pero conviene confirmarlo.
3. Pegar las capturas y los diagramas en los recuadros amarillos. El generador no incrusta
   imágenes a propósito: cuáles son buenas capturas es un criterio humano.

La lista completa de capturas, numerada y con su ubicación, está en la sección 12 de la fuente en
markdown.
