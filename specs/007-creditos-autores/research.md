# Research — Spec 007, módulo de créditos de autores

> Decisiones de diseño con sus alternativas descartadas. Cada decisión se refiere desde
> [`plan.md`](plan.md). Las alternativas se conservan: si una decisión se revierte, aquí está el
> razonamiento original.

---

## D1 — Persistencia de los autores

**Decisión:** entidad `Credit` en PostgreSQL, con migración de Entity Framework Core.

| Alternativa | Ventajas | Por qué se descarta |
|---|---|---|
| Archivo JSON versionado en el repositorio | Sin migración, sin tabla, revisable en el PR | Cambiar un autor exige un despliegue; ninguna otra parte del sistema lee contenido de archivos del repositorio en tiempo de ejecución |
| Sección en la configuración de la aplicación | Igual de simple; ya hay precedente | Un arreglo de objetos en configuración es incómodo de mantener y no admite fotografías asociadas |
| **Entidad en base de datos (elegida)** | Mantenimiento sin despliegue; encaja con la arquitectura por capas vigente; permite asociar archivos | Requiere migración y capa de repositorio |

El factor decisivo es la fotografía: asociar un archivo binario a un registro exige una clave
foránea, y eso pide una tabla.

## D2 — Almacenamiento de las fotografías

**Decisión:** reutilizar el almacenamiento de archivos existente. El autor guarda una referencia
opcional al archivo, y la imagen se recupera por el recurso público de descarga ya disponible.

| Alternativa | Por qué se descarta |
|---|---|
| Guardar una URL externa en texto libre | Es el patrón que ya se usa para la imagen de perfil, pero deja la disponibilidad de la imagen fuera del control del sistema y no permite validar el contenido |
| Tabla nueva de imágenes del módulo | Duplica una capacidad que ya existe y ya está probada |
| Servir las imágenes desde la carpeta pública del frontend | El alcance es backend; además obliga a desplegar para cambiar una foto |

**Consecuencia a documentar:** el archivo almacenado requiere un propietario, y los autores no son
necesariamente usuarios del sistema. Se resuelve asignando como propietario al administrador que
realiza la carga. Queda explícito en el modelo de datos para que nadie lo interprete como que el
autor es un usuario.

## D3 — Ubicación del mensaje del equipo

**Decisión:** en la configuración de la aplicación, bajo una sección propia, junto con el título de
la sección de créditos.

**Razón:** es un texto único que describe al proyecto, no a una persona. Persistirlo exigiría una
tabla de una sola fila, un patrón que el proyecto no usa en ninguna otra parte.

**Contrapartida aceptada:** cambiar el mensaje exige un despliegue. Si más adelante se requiere
editarlo desde la interfaz de administración, la decisión se revisa y el mensaje pasa a ser una
entidad. No se anticipa esa necesidad ahora.

**Alternativa descartada:** una entidad de una sola fila con título y mensaje. Se descarta por
introducir un registro singleton que hay que garantizar único a nivel de aplicación, sin beneficio
frente a la configuración.

## D4 — Estrategia de baja

**Decisión:** baja lógica mediante un indicador de actividad, consistente con el tratamiento que el
sistema ya da a los usuarios.

**Razón:** conserva la fotografía asociada y evita romper referencias. Además mantiene una sola
convención de borrado en el proyecto en lugar de mezclar borrado físico y lógico según el módulo.

**Alternativa descartada:** borrado físico. Obligaría a decidir qué hacer con el archivo asociado y
a definir el comportamiento en cascada, complejidad innecesaria para este módulo.

## D5 — Orden de presentación

**Decisión:** campo entero explícito de orden, con desempate por identificador.

| Alternativa | Por qué se descarta |
|---|---|
| Orden alfabético por nombre | No es el criterio que se quiere; el equipo decide el orden |
| Orden por fecha de alta | Frágil: reordenar exigiría recrear registros |

El desempate por identificador es obligatorio: sin él, dos autores con el mismo valor de orden
producirían un resultado no determinista entre consultas, y la prueba automatizada sería
intermitente.

## D6 — Forma de la respuesta pública

**Decisión:** un único recurso que devuelve un objeto con título, mensaje y arreglo de autores.

**Razón:** el consumidor previsto es una sola pantalla. Exponer el mensaje y la lista por separado
obligaría a dos peticiones para pintar una página.

**Alternativa descartada:** devolver solo el arreglo de autores y dejar el mensaje en el frontend.
Se descarta porque duplicaría el contenido en dos lugares y el frontend quedaría desactualizado
cada vez que cambie el mensaje.

## D7 — Registro en la bitácora de acciones

**Decisión:** no integrar el módulo con la bitácora en esta iteración.

**Razón:** el enumerado de acciones de la bitácora tiene valores fijos que corresponden a módulos
existentes. Agregar valores para créditos cambia un tipo compartido por varios módulos y sus
pruebas, un cambio transversal que merece su propia especificación y no debe entrar de contrabando
en un módulo de créditos.

**Consecuencia:** las altas y bajas de autores no quedan auditadas. Se acepta por tratarse de
contenido no sensible y de bajo volumen de cambio. Queda anotado en la sección de alcance del spec.

## D8 — Sin dependencias nuevas

**Decisión:** el módulo se implementa exclusivamente con lo que el proyecto ya tiene.

**Verificación:** el archivo de proyecto del backend no debe cambiar en el PR de implementación.
Es un criterio de éxito medible, no una intención.
