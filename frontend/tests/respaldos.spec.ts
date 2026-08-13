import { test, expect, Page } from '@playwright/test';

const RESPALDOS_URL = 'http://localhost:3000/usuarios/respaldos';
const JOB_ID = '11111111-2222-3333-4444-555555555555';

const backupsFixture = [
  { fileName: 'backup_20260813_153714.sql', createdAt: '2026-08-13T15:37:14Z', fileSizeBytes: 18651 },
  { fileName: 'backup_20260812_090000.sql', createdAt: '2026-08-12T09:00:00Z', fileSizeBytes: 2048 },
];

/** Sesion simulada: las paginas de administracion leen el token de localStorage. */
async function authenticate(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('token', 'fake-token');
  });
}

/** Mockea el listado. El patron `**` cubre la URL absoluta del backend. */
async function mockList(page: Page, body: unknown, status = 200) {
  await page.route('**/api/Admin/backups', async route => {
    if (route.request().method() !== 'GET') return route.fallback();
    await route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) });
  });
}

/** El POST solo encola: responde 202 con un trabajo todavia sin terminar. */
async function mockEncolar(page: Page, status = 202) {
  await page.route('**/api/Admin/backup', async route => {
    await route.fulfill({
      status,
      contentType: 'application/json',
      body: JSON.stringify({ id: JOB_ID, status: 'Pendiente' }),
    });
  });
}

/** Devuelve los estados indicados, uno por consulta; repite el ultimo al agotarse. */
async function mockEstadosDelTrabajo(page: Page, estados: unknown[]) {
  let consulta = 0;
  await page.route('**/api/Admin/backup/jobs/**', async route => {
    const cuerpo = estados[Math.min(consulta, estados.length - 1)];
    consulta++;
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(cuerpo),
    });
  });
}

test.describe('Panel de respaldos', () => {
  test('muestra la lista de respaldos existentes', async ({ page }) => {
    await authenticate(page);
    await mockList(page, backupsFixture);

    await page.goto(RESPALDOS_URL);

    await expect(page.getByTestId('backups-title')).toBeVisible();
    await expect(page.getByTestId('backup-row')).toHaveCount(2);
    await expect(page.getByTestId('backup-filename').first()).toHaveText('backup_20260813_153714.sql');
    await expect(page.getByTestId('backups-count')).toHaveText('2 archivos');
  });

  test('formatea el tamano en unidades legibles y no en bytes crudos', async ({ page }) => {
    await authenticate(page);
    await mockList(page, backupsFixture);

    await page.goto(RESPALDOS_URL);

    // 18651 B -> 18.2 KB ; 2048 B -> 2.0 KB
    await expect(page.getByTestId('backup-size').first()).toHaveText('18.2 KB');
    await expect(page.getByTestId('backup-size').nth(1)).toHaveText('2.0 KB');
  });

  test('muestra estado vacio cuando no hay respaldos', async ({ page }) => {
    await authenticate(page);
    await mockList(page, []);

    await page.goto(RESPALDOS_URL);

    await expect(page.getByTestId('backups-empty')).toBeVisible();
    await expect(page.getByTestId('backup-row')).toHaveCount(0);
  });

  test('advierte que los respaldos pueden perderse al redesplegar', async ({ page }) => {
    await authenticate(page);
    await mockList(page, []);

    await page.goto(RESPALDOS_URL);

    await expect(page.getByTestId('backups-persistence-warning')).toBeVisible();
  });

  // ───────────────────────────────────────────────────────────────
  // Flujo asincrono (issue #162)
  // ───────────────────────────────────────────────────────────────

  test('espera a que el trabajo termine antes de refrescar la lista', async ({ page }) => {
    await authenticate(page);

    // El archivo NO existe hasta que el trabajo se reporta Completado. Sin esto la
    // prueba pasaria tambien con el codigo defectuoso, que recargaba de inmediato:
    // el listado devolveria el archivo aunque el servidor no lo hubiera escrito.
    let trabajoTerminado = false;

    await page.route('**/api/Admin/backups', async route => {
      if (route.request().method() !== 'GET') return route.fallback();
      const body = trabajoTerminado ? [backupsFixture[0]] : [];
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
    });

    await mockEncolar(page);

    let consultas = 0;
    await page.route('**/api/Admin/backup/jobs/**', async route => {
      consultas++;
      // Sigue en proceso en la primera consulta; termina en la segunda.
      const completado = consultas >= 2;
      if (completado) trabajoTerminado = true;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(
          completado
            ? { id: JOB_ID, status: 'Completado', fileName: backupsFixture[0].fileName }
            : { id: JOB_ID, status: 'EnProceso' },
        ),
      });
    });

    await page.goto(RESPALDOS_URL);
    await expect(page.getByTestId('backups-empty')).toBeVisible();

    await page.getByTestId('generate-backup-button').click();

    // Antes se recargaba nada mas recibir el 202, con el archivo aun sin existir.
    await expect(page.getByTestId('backup-row')).toHaveCount(1);
    await expect(page.getByTestId('backup-filename')).toHaveText(backupsFixture[0].fileName);
  });

  test('el boton sigue mostrando actividad mientras el trabajo no termina', async ({ page }) => {
    await authenticate(page);
    await mockList(page, []);
    await mockEncolar(page);
    // El trabajo nunca sale de EnProceso durante la comprobacion.
    await mockEstadosDelTrabajo(page, [{ id: JOB_ID, status: 'EnProceso' }]);

    await page.goto(RESPALDOS_URL);
    await page.getByTestId('generate-backup-button').click();

    // El sintoma reportado era que el indicador terminaba al instante, con el
    // respaldo todavia ejecutandose en el servidor.
    await page.waitForTimeout(2500);
    await expect(page.getByTestId('generate-backup-button')).toBeDisabled();
  });

  test('muestra el motivo cuando el trabajo termina en Fallido', async ({ page }) => {
    await authenticate(page);
    await mockList(page, []);
    await mockEncolar(page);
    await mockEstadosDelTrabajo(page, [
      { id: JOB_ID, status: 'Fallido', error: 'No se pudo generar el respaldo.' },
    ]);

    await page.goto(RESPALDOS_URL);
    await page.getByTestId('generate-backup-button').click();

    await expect(page.getByTestId('backups-error')).toBeVisible();
    await expect(page.getByTestId('backups-error')).toContainText('No se pudo generar el respaldo');
  });

  test('muestra el error cuando el encolado falla', async ({ page }) => {
    await authenticate(page);
    await mockList(page, []);

    await page.route('**/api/Admin/backup', async route => {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'No se pudo generar el respaldo de la base de datos.' }),
      });
    });

    await page.goto(RESPALDOS_URL);
    await page.getByTestId('generate-backup-button').click();

    await expect(page.getByTestId('backups-error')).toBeVisible();
    await expect(page.getByTestId('backups-error')).toContainText('No se pudo generar el respaldo');
  });

  test('muestra el error cuando el listado falla', async ({ page }) => {
    await authenticate(page);
    await mockList(page, { message: 'no autorizado' }, 403);

    await page.goto(RESPALDOS_URL);

    await expect(page.getByTestId('backups-error')).toBeVisible();
  });

  // ───────────────────────────────────────────────────────────────
  // Descarga
  // ───────────────────────────────────────────────────────────────

  test('la descarga pide el archivo con cabecera Authorization', async ({ page }) => {
    await authenticate(page);
    await mockList(page, [backupsFixture[0]]);

    let authHeader: string | undefined;
    await page.route(`**/api/Admin/backups/${backupsFixture[0].fileName}`, async route => {
      authHeader = route.request().headers()['authorization'];
      await route.fulfill({
        status: 200,
        contentType: 'application/octet-stream',
        body: '-- volcado de prueba',
      });
    });

    await page.goto(RESPALDOS_URL);

    const downloadPromise = page.waitForEvent('download');
    await page.getByTestId('backup-download-button').click();
    const download = await downloadPromise;

    // Lo que se verifica no es solo que descargue, sino que lleve el token:
    // un <a href> plano devolveria 401 porque el navegador no lo adjunta.
    expect(authHeader).toBe('Bearer fake-token');
    expect(download.suggestedFilename()).toBe(backupsFixture[0].fileName);
  });

  test('acepta descargar respaldos con el formato de nombre anterior', async ({ page }) => {
    // Los archivos creados antes del cambio a segundos tienen cuatro digitos.
    // Si la lista blanca del dominio no los aceptara, dejarian de poder descargarse.
    await authenticate(page);
    const antiguo = { fileName: 'backup_20260813_2017.sql', createdAt: '2026-08-13T20:17:00Z', fileSizeBytes: 20480 };
    await mockList(page, [antiguo]);

    await page.route(`**/api/Admin/backups/${antiguo.fileName}`, async route => {
      await route.fulfill({ status: 200, contentType: 'application/octet-stream', body: '-- antiguo' });
    });

    await page.goto(RESPALDOS_URL);

    const downloadPromise = page.waitForEvent('download');
    await page.getByTestId('backup-download-button').click();
    const download = await downloadPromise;

    expect(download.suggestedFilename()).toBe(antiguo.fileName);
  });
});
