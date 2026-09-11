# Research: Guía interactiva de producto por rol

## R1. Librería de recorridos

- **Decision**: `driver.js`.
- **Rationale**: Licencia MIT, sin dependencias y agnóstica del framework, así que no compite con
  Chakra; API declarativa de pasos (`driver({ steps }).drive()`), resaltado de elementos,
  progreso, navegación con teclado y ganchos (`onPopoverRender`, `onDestroyed`). Elegida por el
  responsable y amparada por la enmienda v1.1.0.
- **Alternatives considered**:
  - *Construirlo con Chakra UI v3 (Popover + Steps)*: sin dependencia nueva, pero habría que
    programar a mano la superposición, el recorte del resaltado, el posicionamiento y el
    desplazamiento hasta el elemento. Más código y más riesgo.
  - *Intro.js*: licencia AGPL/comercial; inadecuada para el proyecto.
  - *React Joyride*: MIT y pensada para React, pero su compatibilidad con React 19 no está
    verificada y trae dependencias de posicionamiento propias.
  - *Shepherd.js*: MIT y completa, pero más pesada y con su propio sistema de estilos.

## R2. Carga solo en cliente

- **Decision**: Importar `driver.js` y su CSS con `import()` dinámico dentro de un `useEffect`
  del componente `TourLauncher` (`"use client"`), solo cuando hay un recorrido que mostrar.
- **Rationale**: Evita errores de SSR (`window is not defined`) y no penaliza la carga inicial.
- **Alternatives considered**: `next/dynamic` con `ssr: false` para todo el componente. Válido,
  pero el botón "Ver guía" debe renderizarse siempre; basta con diferir la librería.

## R3. Persistencia del estado

- **Decision**: `localStorage` con la clave `gsp-tour:<tourId>:v<version>:<usuario>` y el valor
  `{ "result": "completed" | "dismissed", "at": "<ISO-8601>" }`. El repositorio envuelve cada
  lectura y escritura en `try/catch` y usa un mapa en memoria como respaldo si el
  almacenamiento no está disponible.
- **Rationale**: Decisión del responsable (sin backend). Incluir la versión en la clave cumple
  FR-008 (una versión nueva se muestra una vez más); incluir el usuario separa a dos personas que
  comparten navegador.
- **Alternatives considered**: Persistir en el backend (columna o tabla, endpoint, migración y
  pruebas): descartado por alcance en *Clarifications*.

## R4. Anclajes estables

- **Decision**: Atributos `data-tour="<id>"` en los elementos de destino.
- **Rationale**: Hoy las pantallas objetivo no tienen ningún `data-testid` y usan estilos en
  línea; un selector por clase o posición se rompería con cualquier cambio visual. Los mismos
  anclajes pueden servir a la suite E2E del piloto 001.
- **Alternatives considered**: Reutilizar `data-testid` (mezcla dos propósitos); selectores CSS
  (frágiles).

## R5. Pasos cuyo elemento no existe

- **Decision**: Antes de iniciar, el controlador filtra los pasos cuyo
  `document.querySelector('[data-tour="…"]')` no existe; el recorrido se construye solo con los
  presentes y el progreso refleja ese total.
- **Rationale**: Cumple FR-006 sin depender del comportamiento interno de la librería ante un
  elemento ausente.

## R6. Dónde corre cada recorrido

- **Decision**: El recorrido se lanza en la primera página autenticada que monta la navegación
  (hoy `/dashboard` tras el login) y se compone sobre todo de anclajes de la navegación, que están
  en todas las páginas; los pasos propios de otra página se omiten por R5. El recorrido no navega
  entre páginas; si el usuario navega, se cierra como omitido.
- **Rationale**: Un recorrido multipágina exige sincronizarse con el router y es frágil; la
  navegación ya muestra los accesos de cada rol.

## R7. Normalización del rol

- **Decision**: `normalizeRole(role: string): TourRole | null` acepta tanto el nombre (`"Client"`,
  `"Professional"`, `"Admin"`) como el valor numérico en texto (`"1"`, `"2"`, `"0"`).
- **Rationale**: `auth-store.ts` guarda el rol como texto del nombre cuando viene del login
  (`setFromAuthResponse`) y como número convertido a texto cuando viene del perfil
  (`setFromProfile` hace `String(data.role)`, con `UserRole` Admin = 0, Client = 1,
  Professional = 2). Sin normalizar, el recorrido fallaría según el camino por el que se cargó la
  sesión.
- **Nota**: La inconsistencia se registra como brecha del producto; corregirla en el store queda
  fuera de este alcance.

## R8. Accesibilidad y pantallas angostas

- **Decision**: Mantener la navegación con teclado de la librería (flechas y Esc); en
  `onPopoverRender` añadir `role="dialog"`, `aria-labelledby` y `aria-describedby` al popover y
  mover el foco a su botón principal. En viewports < 768 px se omite `element` en cada paso para
  mostrar un popover centrado sin resaltado.
- **Rationale**: FR-009 y el caso borde de pantallas angostas.
