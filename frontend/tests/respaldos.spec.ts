import { test, expect, Page } from '@playwright/test';

const RESPALDOS_URL = 'http://localhost:3000/usuarios/respaldos';

const backupsFixture = [
  { fileName: 'backup_20260813_1537.sql', createdAt: '2026-08-13T15:37:14Z', fileSizeBytes: 18651 },
  { fileName: 'backup_20260812_0900.sql', createdAt: '2026-08-12T09:00:00Z', fileSizeBytes: 2048 },
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

test.describe('Panel de respaldos', () => {
  test('muestra la lista de respaldos existentes', async ({ page }) => {
    await authenticate(page);
    await mockList(page, backupsFixture);

    await page.goto(RESPALDOS_URL);

    await expect(page.getByTestId('backups-title')).toBeVisible();
    await expect(page.getByTestId('backup-row')).toHaveCount(2);
    await expect(page.getByTestId('backup-filename').first()).toHaveText('backup_20260813_1537.sql');
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

  test('generar un respaldo refresca la lista', async ({ page }) => {
    await authenticate(page);

    let listCalls = 0;
    await page.route('**/api/Admin/backups', async route => {
      if (route.request().method() !== 'GET') return route.fallback();
      listCalls += 1;
      // La primera carga esta vacia; tras generar, la lista ya trae el archivo.
      const body = listCalls === 1 ? [] : [backupsFixture[0]];
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
    });

    await page.route('**/api/Admin/backup', async route => {
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify(backupsFixture[0]),
      });
    });

    await page.goto(RESPALDOS_URL);
    await expect(page.getByTestId('backups-empty')).toBeVisible();

    await page.getByTestId('generate-backup-button').click();

    await expect(page.getByTestId('backup-row')).toHaveCount(1);
    await expect(page.getByTestId('backup-filename')).toHaveText('backup_20260813_1537.sql');
  });

  test('muestra el error cuando la generacion falla', async ({ page }) => {
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

  test('la descarga pide el archivo con cabecera Authorization', async ({ page }) => {
    await authenticate(page);
    await mockList(page, [backupsFixture[0]]);

    let authHeader: string | undefined;
    await page.route('**/api/Admin/backups/backup_20260813_1537.sql', async route => {
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
    expect(download.suggestedFilename()).toBe('backup_20260813_1537.sql');
  });
});
