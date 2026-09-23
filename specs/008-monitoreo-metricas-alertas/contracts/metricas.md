# Contrato — métricas de negocio

> Qué reglas debe cumplir toda métrica que emita este proyecto, y cómo se añade una nueva sin romper
> los tableros ni las alertas que ya dependen de las existentes.
>
> El catálogo vigente está en [data-model.md](../data-model.md). Este documento fija las reglas; ese
> otro, el inventario.

---

## 1. Nombres

```
gsp.<dominio>.<hecho-en-plural>
```

- **`gsp`** es el espacio del proyecto. Lo llevan todas las métricas propias y ninguna ajena, para
  que una sola consulta separe lo nuestro de lo que aporta la plataforma.
- **`<dominio>`** es el área de negocio: `solicitudes`, `autenticacion`, `usuarios`, `respaldos`.
- **`<hecho>`** es lo que ocurrió, en plural y en pasado o sustantivado: `creadas`, `intentos`,
  `cambios_estado`. **Nunca** el nombre de un método ni de una clase: el código se renombra, y una
  métrica renombrada rompe todo lo que la consulta.

Las unidades no se ponen en el nombre; las declara el instrumento y el sufijo lo añade el
exportador. `gsp.respaldos.duracion` medido en segundos se consulta como
`gsp_respaldos_duracion_seconds`.

## 2. Etiquetas

### Obligatorio

- Toda etiqueta tiene un **dominio cerrado**, declarado en el catálogo antes de escribirse el
  código.
- Todo valor de etiqueta es una **constante del código**, no un dato de entrada transformado.
- Toda métrica lleva al menos una etiqueta `resultado`, porque contar cuántas veces ocurrió algo sin
  saber si salió bien casi nunca responde una pregunta útil.

### Prohibido

| No se etiqueta con | Por qué |
|---|---|
| Correo, nombre, teléfono | Dato personal. La métrica cuenta; el **registro** identifica |
| Identificador de usuario, solicitud, servicio o pago | Cardinalidad ilimitada: una serie nueva por cada entidad del sistema |
| Dirección IP | Dato personal y cardinalidad ilimitada |
| Rutas o cadenas con parámetros sustituidos | Cardinalidad ilimitada. Las métricas de peticiones usan la **plantilla** de ruta, no la dirección concreta |
| Mensajes de excepción | Texto libre: cardinalidad ilimitada y posible filtración de detalle interno |
| Marcas de tiempo | La serie temporal ya tiene su eje de tiempo |

La regla operativa es una sola pregunta: **¿cuántos valores distintos puede tomar esta etiqueta a lo
largo de un año?** Si la respuesta no es un número pequeño escrito en el catálogo, no es una
etiqueta: es un registro.

## 3. Dónde se emite

| Capa | ¿Emite métricas? | Por qué |
|---|---|---|
| Controlador | **No** | Solo mapea HTTP. Lo que ocurre ahí ya lo cuenta la instrumentación automática |
| Servicio | **Sí** | Es donde está la decisión de negocio que se quiere contar |
| Repositorio | **No** | Solo accede a datos; el tiempo de consulta ya lo cubren las trazas del controlador de base de datos |

Emitir desde un controlador mide *lo que entró por HTTP*; emitir desde un servicio mide *lo que el
negocio decidió*. Esa diferencia es exactamente la razón de existir de estas métricas: si la API
responde 200 y no se creó la solicitud, solo la segunda lo detecta.

## 4. Comportamiento

1. El medidor se consume **por inyección**, a través del contrato en `Interfaces/`. Nunca se crea
   uno dentro de un servicio.
2. La implementación es de **instancia única** y libera sus recursos al cerrar.
3. Los instrumentos se crean **una sola vez**, en el constructor. Crear un instrumento por llamada
   es un error silencioso que se paga en memoria.
4. **Ningún método puede propagar una excepción.** Una métrica que rompe un inicio de sesión es peor
   que no tener métrica. Hay una prueba que lo afirma.
5. Los métodos devuelven `void`. Una métrica no es una decisión: nadie debe poder ramificar según lo
   que devuelva.

## 5. Cómo se añade una métrica nueva

1. **Escribirla en el catálogo primero** ([data-model.md](../data-model.md)): nombre, tipo,
   etiquetas con sus valores posibles y para qué sirve. Si no se puede enunciar para qué sirve, no
   se añade.
2. Comprobar la cardinalidad: multiplicar los dominios de todas sus etiquetas. Si el producto no es
   un número pequeño, replantear las etiquetas.
3. Añadir el método al contrato y su implementación.
4. Añadir la prueba unitaria: que emite, con qué etiquetas, y que **no** lleva datos personales.
5. Añadirla a la lista del guion de verificación, que es lo que hace que su ausencia falle en
   integración continua en lugar de descubrirse meses después.
6. Si alimenta un nivel de servicio o una alerta, definir la expresión como regla de registro para
   que tablero y alerta no la calculen por separado.

## 6. Cómo se retira una métrica

No se borra sin más: puede estar alimentando un tablero o una alerta.

1. Buscarla en las reglas, en los tableros y en el guion de verificación.
2. Retirar primero lo que la consume.
3. Retirar la emisión y su prueba.
4. Anotarlo en el catálogo. Una métrica que desaparece sin explicación se reinventa con otro nombre
   seis meses después.
