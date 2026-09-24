# Contrato — despliegue verificado y reversión

> Qué entradas acepta el flujo de despliegue, qué garantiza, cuándo falla y cómo se revierte.
> Complementa el [contrato de workflows de la spec 003](../../003-infraestructura-como-codigo/contracts/workflows.md),
> que describía este flujo cuando todavía se planeaba construir e importar la imagen; aquí se
> concreta contra el mecanismo que la plataforma ofrece hoy.

---

## 1. Interfaz

| Disparador | Cuándo | Entrada |
|---|---|---|
| Automático | Al terminar en verde la construcción de la imagen sobre la rama de integración | Ninguna: despliega ese commit |
| Manual | A petición | `sha`: commit a desplegar. Vacío significa el último de la rama de integración |
| Manual (reversión) | A petición | `sha`: el último commit sano conocido |

**No hay un disparador de reversión distinto.** Revertir es desplegar un commit anterior, con la
misma verificación. Eso garantiza que el camino de reversión se ejercita en cada despliegue, en
lugar de ser una ruta que nadie ha recorrido hasta el día del incidente.

## 2. Precondiciones

| # | Condición | Si no se cumple |
|---|---|---|
| P1 | El secreto del disparador existe | El trabajo termina explicando qué secreto falta, sin intentar desplegar |
| P2 | Las cuatro comprobaciones de integración continua concluyeron **en verde para ese mismo commit** | El trabajo se detiene nombrando la comprobación que falló o que sigue pendiente |
| P3 | El ambiente protegido aprobó la ejecución | El trabajo queda en espera de revisión |
| P4 | No hay otro despliegue en curso al mismo ambiente | Este espera; **no** cancela al que está corriendo |

P2 es la que impide que el flujo automático deje pasar un commit con pruebas rojas: encadenar el
despliegue a *un solo* flujo previo solo demuestra que ese flujo pasó.

## 3. Contrato de la verificación

1. **Espera inicial** antes del primer sondeo. Sin ella se sondearía la instancia **anterior**, que
   responde sana, y el despliegue se daría por bueno sin haber ocurrido. Es el modo de fallo más
   probable de todo el diseño.
2. **Sondeo de la sonda de preparación**, no la de vida: preparación confirma además que la base de
   datos es alcanzable y, por tanto, que las migraciones se aplicaron.
3. **Éxito** = varias respuestas sanas **consecutivas**. Una sola puede ser un rebote durante el
   reinicio.
4. **Plazo** dimensionado para cubrir la construcción de la imagen en la plataforma más el arranque
   en frío. Es sustancialmente mayor que el de la verificación local, y esa diferencia es
   deliberada, no un descuido.
5. **Fallo** = el plazo se agota sin alcanzar el éxito. Termina en rojo, imprime la última respuesta
   recibida y dónde consultar los registros del servicio.

## 4. Garantías

| Garantía | Cómo se cumple |
|---|---|
| El secreto nunca aparece en la salida | Se enmascara antes de usarse y la salida del disparo se descarta |
| Un despliegue fallido es visible | El trabajo termina en rojo; nunca se degrada un fallo a aviso |
| La versión desplegada es trazable al commit | El commit es entrada y salida del flujo, y queda en el resumen |
| Dos despliegues no se solapan | Agrupación de concurrencia sin cancelación |
| Quien revierte sabe a qué commit volver | Cada ejecución registra el commit que estaba desplegado **antes** |

## 5. Salida

El resumen de cada ejecución contiene, siempre:

- Commit desplegado y commit anterior.
- Duración total y resultado de la verificación.
- **La orden de reversión ya escrita**, lista para copiar. En un incidente nadie debería estar
  redactando parámetros.
- La advertencia de que **una migración destructiva no se revierte redesplegando**: la imagen vuelve
  atrás, los datos no. La recuperación pasa por los guiones de respaldo, que son destructivos y
  exigen confirmación explícita (constitución, Principio IV).

## 6. Lo que este contrato no cubre

- **Migraciones destructivas**, por lo dicho arriba.
- **Varias réplicas.** Con una sola instancia, la aplicación de migraciones al arrancar es segura.
  Antes de escalar hay que resolver que varias instancias no migren a la vez; está señalado en la
  spec 003 y sigue pendiente.
- **Promoción entre ambientes.** Hoy hay un solo ambiente desplegable. La separación entre pruebas y
  producción es el objeto de la spec 003, no de esta.
