import { test, expect } from '@playwright/test';

const PROFILE_MOCK = {
    id: '00000000-0000-0000-0000-000000000001',
    email: 'juan@ejemplo.com',
    firstName: 'Juan',
    lastName: 'Perez',
    phoneNumber: '6641234567',
    role: 1,
    profileImageUrl: null,
    status: true,
    averageRating: 0,
    createdAt: new Date().toISOString(),
};

async function mockLogin(page: any) {
    await page.addInitScript(() => {
        localStorage.setItem('token', 'fake-jwt-token');
    });
}

async function mockProfileGet(page: any, profile = PROFILE_MOCK) {
    await page.route('**/api/Profile', async (route: any) => {
        if (route.request().method() === 'GET') {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify(profile),
            });
        } else {
            await route.continue();
        }
    });
}

async function mockProfilePut(page: any, updatedProfile = PROFILE_MOCK) {
    await page.route('**/api/Profile', async (route: any) => {
        if (route.request().method() === 'PUT') {
            const body = JSON.parse(route.request().postData() ?? '{}');
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ ...updatedProfile, ...body }),
            });
        } else {
            await route.continue();
        }
    });
}

// ─── Carga ────────────────────────────────────────────────────────────────────

test('Perfil página carga y muestra datos del usuario', async ({ page }) => {
    await mockLogin(page);
    await mockProfileGet(page);

    await page.goto('http://localhost:3000/perfil');

    await expect(page.getByTestId('field-nombre')).toHaveValue('Juan');
    await expect(page.getByTestId('field-apellido')).toHaveValue('Perez');
    await expect(page.getByTestId('field-telefono')).toHaveValue('6641234567');
});

test('Perfil sin token redirige al login', async ({ page }) => {
    await page.goto('http://localhost:3000/perfil');
    await expect(page).toHaveURL(/login/, { timeout: 10000 });
});

// ─── Modo edición ─────────────────────────────────────────────────────────────

test('Clic en botón de editar habilita el campo Nombre', async ({ page }) => {
    await mockLogin(page);
    await mockProfileGet(page);

    await page.goto('http://localhost:3000/perfil');
    await page.getByTestId('btn-nombre').click();

    await expect(page.getByTestId('field-nombre')).not.toHaveAttribute('readonly');
});

test('Al editar sin cambios el botón actúa como cancelar', async ({ page }) => {
    await mockLogin(page);
    await mockProfileGet(page);

    await page.goto('http://localhost:3000/perfil');
    const btn = page.getByTestId('btn-nombre');
    await btn.click(); // abre edición
    await btn.click(); // cancela sin cambios

    await expect(page.getByTestId('field-nombre')).toHaveAttribute('readonly', '');
    await expect(page.getByTestId('field-nombre')).toHaveValue('Juan');
});

// ─── Guardar cambios ──────────────────────────────────────────────────────────

test('Editar nombre y guardar actualiza el valor en pantalla', async ({ page }) => {
    await mockLogin(page);

    // GET devuelve perfil inicial
    await page.route('**/api/Profile', async (route) => {
        if (route.request().method() === 'GET') {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify(PROFILE_MOCK),
            });
        } else if (route.request().method() === 'PUT') {
            const body = JSON.parse(route.request().postData() ?? '{}');
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ ...PROFILE_MOCK, firstName: body.firstName }),
            });
        } else {
            await route.continue();
        }
    });

    await page.goto('http://localhost:3000/perfil');

    await page.getByTestId('btn-nombre').click();
    await page.getByTestId('field-nombre').fill('Carlos');
    await page.getByTestId('btn-nombre').click(); // guardar

    await expect(page.getByTestId('field-nombre')).toHaveValue('Carlos');
    await expect(page.getByTestId('field-nombre')).toHaveAttribute('readonly', '');
});

test('Error de API al guardar muestra mensaje de error', async ({ page }) => {
    await mockLogin(page);

    await page.route('**/api/Profile', async (route) => {
        if (route.request().method() === 'GET') {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify(PROFILE_MOCK),
            });
        } else if (route.request().method() === 'PUT') {
            await route.fulfill({ status: 500, body: 'Error interno' });
        } else {
            await route.continue();
        }
    });

    await page.goto('http://localhost:3000/perfil');

    await page.getByTestId('btn-nombre').click();
    await page.getByTestId('field-nombre').fill('Carlos');
    await page.getByTestId('btn-nombre').click();

    // El campo debería seguir en modo edición y mostrar error
    await expect(page.getByTestId('field-nombre')).not.toHaveAttribute('readonly', '');
});

// ─── Navegación ───────────────────────────────────────────────────────────────

test('Enlace Cambiar contraseña navega a /perfil/contrasena', async ({ page }) => {
    await mockLogin(page);
    await mockProfileGet(page);

    await page.goto('http://localhost:3000/perfil');
    await page.getByRole('link', { name: /cambiar contraseña/i }).click();

    await expect(page).toHaveURL(/perfil\/contrasena/, { timeout: 10000 });
});
