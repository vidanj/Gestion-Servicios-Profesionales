# Operation Checklist: Monitoreo y despliegue

**Purpose**: Comprobaciones de operación antes de dar por terminada la implementación.
**Created**: 2026-09-23
**Feature**: [spec.md](../spec.md)

> Esta lista no revisa el código: revisa que lo entregado **se pueda operar**. Varias de sus
> entradas solo puede marcarlas el responsable, porque dependen de accesos que nadie más tiene.

## Secretos y accesos

- [ ] El secreto del disparador de despliegue existe y **no** aparece en ningún archivo del repositorio
- [ ] La dirección pública a sondear está configurada como variable, no escrita en un archivo versionado
- [ ] El ambiente protegido existe y tiene al menos un revisor obligatorio
- [ ] La contraseña del tablero está definida y **no** es la de por defecto
- [ ] Las variables nuevas están en `.env.example` con valores de ejemplo, nunca reales
- [ ] Ningún guion contiene credenciales

## Configuración del entorno

- [ ] Todas las imágenes están fijadas a una versión exacta; ninguna etiqueta móvil
- [ ] Los datos de las tres herramientas con estado persisten en volúmenes nombrados
- [ ] El entorno de monitoreo arranca con la aplicación detenida
- [ ] El archivo de composición de la aplicación **no** fue modificado
- [ ] Los puertos publicados no chocan con los de la aplicación ni con los del análisis estático
- [ ] El límite de memoria del colector está declarado

## Verificación funcional

- [ ] El guion de verificación pasa con el catálogo completo
- [ ] Las reglas de alerta superan su validación y sus casos de prueba
- [ ] Una caída provocada dispara la alerta crítica dentro del plazo declarado
- [ ] Restablecer el servicio resuelve la alerta sin intervención
- [ ] Un arranque en frío simulado **no** dispara ninguna alerta crítica
- [ ] Ninguna serie de negocio contiene etiquetas con datos personales
- [ ] La cardinalidad tras una hora de tráfico está por debajo del límite declarado

## Despliegue

- [ ] El comportamiento del parámetro de commit se comprobó **a mano** antes de automatizarlo
- [ ] Un commit con comprobaciones en rojo no llega a desplegarse
- [ ] Un despliegue que no queda sano termina en rojo, no en aviso
- [ ] Se ejecutó **un despliegue real** y quedó registrada su duración
- [ ] Se ejecutó **una reversión real** y quedó registrada su duración
- [ ] El resumen de la ejecución incluye la orden de reversión y la advertencia sobre migraciones

## Documentación y cierre

- [ ] El README documenta las variables nuevas y cómo levantar el entorno
- [ ] Los documentos de planeación previos dejaron de describir como "planeado" lo que ya existe
- [ ] El issue superado por cambio de enfoque quedó cerrado **con su explicación**
- [ ] El issue absorbido quedó enlazado desde el PR
- [ ] El documento entregable incluye los apartados reservados de los puntos 2 y 3
- [ ] `git status` limpio: ningún artefacto generado quedó sin ignorar

## Límites conocidos, aceptados por escrito

- [ ] Queda dicho que el entorno de monitoreo es local y no observa el servicio publicado por dentro
- [ ] Queda dicho que el propio monitoreo no tiene alta disponibilidad
- [ ] Queda dicho que las alertas no se envían a ningún destino externo, y por qué
- [ ] Queda dicho que una migración destructiva no se revierte redesplegando
