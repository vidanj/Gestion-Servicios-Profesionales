import { defineConfig, devices } from '@playwright/test';

const EN_CI = !!process.env.CI;
const BASE_URL = 'http://localhost:3000';

/**
 * Hasta ahora el repositorio no tenia este archivo: las specs corrian con los valores
 * por defecto y dependian de que alguien levantara el servidor a mano. Sin `webServer`
 * no habia forma de ejecutarlas en integracion continua.
 */
export default defineConfig({
  testDir: './tests',

  // Las specs actuales usan URLs absolutas; baseURL queda disponible para las nuevas.
  use: {
    baseURL: BASE_URL,
    // Solo al reintentar: guardar traza de cada ejecucion encarece mucho el job.
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  // En CI los flakes se reintentan; en local se ven tal cual para no ocultarlos.
  retries: EN_CI ? 2 : 0,

  // Un solo worker en CI: las specs comparten el puerto 3000 y varias asumen que
  // nadie mas esta interactuando con la aplicacion.
  workers: EN_CI ? 1 : undefined,

  // Evita que un `test.only` olvidado deje pasar el resto sin ejecutarse.
  forbidOnly: EN_CI,

  reporter: EN_CI
    ? [['list'], ['html', { open: 'never' }]]
    : [['list']],

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // En CI se sirve la compilacion de produccion, que es lo que realmente se despliega.
  // En local basta el servidor de desarrollo, que no exige compilar antes.
  webServer: {
    command: EN_CI ? 'npm run start' : 'npm run dev',
    url: BASE_URL,
    reuseExistingServer: !EN_CI,
    timeout: 120_000,
  },
});
