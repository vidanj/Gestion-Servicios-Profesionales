# Checklist de seguridad — Spec 007

> Dominio: seguridad. Se marca durante la revisión del plan y se vuelve a verificar en el PR de
> implementación. Varios puntos provienen de defectos ya detectados en la línea base del proyecto.

## Autorización

- [ ] El recurso de lectura pública no expone datos que requieran sesión
- [ ] Toda operación de escritura exige rol administrador de forma explícita
- [ ] La consulta individual está restringida: no permite enumerar registros dados de baja desde fuera
- [ ] Ningún recurso toma la identidad del solicitante del cuerpo de la petición

## Exposición de información

- [ ] Las respuestas de error no incluyen el mensaje de la excepción ni trazas
- [ ] El objeto de salida no expone identificadores de usuarios del sistema
- [ ] El objeto de salida no expone el indicador de actividad ni campos internos de auditoría
- [ ] El detalle técnico de los fallos queda en el registro de operación, ligado a su traza

## Entrada de datos

- [ ] Toda longitud máxima se valida antes de persistir
- [ ] El enlace de perfil se valida como dirección absoluta con esquema seguro
- [ ] Los objetos de entrada no aceptan el campo de fotografía, para que no se salte la validación de contenido
- [ ] Los objetos de entrada no aceptan el identificador del registro

## Archivos

- [ ] La validación es por contenido binario, nunca por extensión ni por tipo declarado
- [ ] Existe un límite de tamaño y se aplica antes de almacenar
- [ ] Un archivo rechazado no deja rastro almacenado
- [ ] El propietario del archivo queda registrado como el administrador que lo carga

## Integridad

- [ ] Borrar un archivo referenciado por un autor falla de forma visible, no vacía el campo en silencio
- [ ] La baja lógica conserva el registro y su archivo asociado
- [ ] El módulo no participa de ninguna cadena de borrado en cascada

## Dependencias

- [ ] No se incorpora ningún paquete nuevo
- [ ] No se reimplementa la validación de imágenes ni el almacenamiento existentes
