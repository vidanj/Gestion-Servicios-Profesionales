# Contrato de UI: recorridos guiados

Contrato entre la feature `src/features/tours/`, las pantallas que la alojan y las pruebas E2E.

## 1. Anclajes `data-tour`

| Valor | Elemento | Archivo |
|---|---|---|
| `nav-catalogo` | Enlace "Catálogos" | `src/components/nav/nav.tsx` |
| `nav-mis-servicios` | Enlace "Mis Servicios" | `src/components/nav/nav.tsx` |
| `nav-solicitudes` | Enlace "Solicitudes" | `src/components/nav/nav.tsx` |
| `nav-administracion` | Enlace "Administración" | `src/components/nav/nav.tsx` |
| `nav-perfil` | Avatar / "Mi Perfil" | `src/components/nav/nav.tsx` |
| `nav-ver-guia` | Botón "Ver guía" (nuevo) | `src/features/tours/presentation/tour-launcher.tsx` |
| `catalogo-lista` | Contenedor de la cuadrícula de servicios | `app/dashboard/page.tsx` |
| `catalogo-tarjeta` | Primera tarjeta de servicio | `app/dashboard/page.tsx` |
| `servicio-formulario` | Formulario de alta de servicio | `app/profesionista/page.tsx` |
| `servicio-lista` | Lista de "Mis servicios" | `app/profesionista/page.tsx` |
| `usuarios-tabla` | Tabla de usuarios | `app/usuarios/page.tsx` |
| `admin-bitacora` / `admin-grafica` / `admin-respaldos` | Accesos a esas pantallas | `app/usuarios/page.tsx` |

**Regla:** un anclaje es un contrato público; renombrarlo o quitarlo exige actualizar
`tours.catalog.ts` y las pruebas en el mismo PR.

## 2. Almacenamiento

- **Clave:** `gsp-tour:<tourId>:v<version>:<usuario>`
- **Valor:** `{"result":"completed"|"dismissed","at":"<ISO-8601>"}`
- Las pruebas pueden sembrar la clave con `page.addInitScript` para simular un recorrido ya visto.

## 3. API de la feature

```ts
// application/use-tour-controller.ts
export type UseTourController = {
  /** Hay un recorrido de la versión vigente sin ver para el usuario actual. */
  hasPending: boolean;
  /** Inicia el recorrido del rol actual desde el primer paso (FR-007). */
  start: (options?: { auto?: boolean }) => Promise<void>;
};

// presentation/tour-launcher.tsx
// Sin props. Renderiza el botón "Ver guía" (data-tour="nav-ver-guia") y, al montarse,
// llama a start({ auto: true }) si hasPending es verdadero.
export default function TourLauncher(): JSX.Element;
```

## 4. Accesibilidad del popover

- `role="dialog"`, `aria-labelledby` (título) y `aria-describedby` (descripción).
- Teclas: → siguiente, ← anterior, Esc cerrar (cuenta como omitido).
- Al abrirse, el foco va al botón principal ("Siguiente" o "Terminar").
