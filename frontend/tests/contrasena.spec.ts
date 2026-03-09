import { test, expect } from '@playwright/test';

async function mockLogin(page: any) {
    await page.addInitScript(() => {
        localStorage.setItem('token', 'fake-jwt-token');
    });
}

async function mockPasswordChange(page: any, status = 200) {
    await page.route('**/api/Profile/password', async (route: any) => {
        if (status === 200) {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ message: 'Contraseña actualizada correctamente.' }),
            });
        } else {
            await route.fulfill({
                status: 400,
                contentType: 'application/json',
                body: JSON.stringify({ message: 'La contraseña actual es incorrecta.' }),
            });
        }
    });
}

// ─── Carga ────────────────────────────────────────────────────────────────────

test('Página de contraseña carga y muestra los campos', async ({ page }) => {
    await mockLogin(page);
    await page.goto('http://localhost:3000/perfil/contrasena');

    await expect(page.getByTestId('current-password-input')).toBeVisible();
    await expect(page.getByTestId('new-password-input')).toBeVisible();
    await expect(page.getByTestId('confirm-password-input')).toBeVisible();
    await expect(page.getByTestId('submit-password-btn')).toBeVisible();
});

test('Contraseña sin token redirige al login', async ({ page }) => {
    await page.goto('http://localhost:3000/perfil/contrasena');
    await expect(page).toHaveURL(/login/, { timeout: 10000 });
});

// ─── Validación de formulario ─────────────────────────────────────────────────

test('Enviar formulario vacío muestra errores de validación', async ({ page }) => {
    await mockLogin(page);
    await page.goto('http://localhost:3000/perfil/contrasena');

    await page.getByTestId('submit-password-btn').click();

    await expect(page.getByText(/obligatori/i).first()).toBeVisible();
});

test('Nueva contraseña menor a 8 caracteres muestra error', async ({ page }) => {
    await mockLogin(page);
    await page.goto('http://localhost:3000/perfil/contrasena');

    await page.getByTestId('current-password-input').fill('password123');
    await page.getByTestId('new-password-input').fill('corta');
    await page.getByTestId('confirm-password-input').fill('corta');
    await page.getByTestId('submit-password-btn').click();

    await expect(page.getByText(/8 caracteres/i)).toBeVisible();
});

test('Contraseñas que no coinciden muestra error', async ({ page }) => {
    await mockLogin(page);
    await page.goto('http://localhost:3000/perfil/contrasena');

    await page.getByTestId('current-password-input').fill('password123');
    await page.getByTestId('new-password-input').fill('NuevaPassword1!');
    await page.getByTestId('confirm-password-input').fill('NuevaPassword2!');
    await page.getByTestId('submit-password-btn').click();

    await expect(page.getByText(/no coinciden/i)).toBeVisible();
});

// ─── Flujo exitoso ────────────────────────────────────────────────────────────

test('Cambio de contraseña exitoso muestra mensaje de confirmación', async ({ page }) => {
    await mockLogin(page);
    await mockPasswordChange(page, 200);

    await page.goto('http://localhost:3000/perfil/contrasena');

    await page.getByTestId('current-password-input').fill('password123');
    await page.getByTestId('new-password-input').fill('NuevaPassword1!');
    await page.getByTestId('confirm-password-input').fill('NuevaPassword1!');
    await page.getByTestId('submit-password-btn').click();

    await expect(page.getByText(/actualizada correctamente/i)).toBeVisible({ timeout: 10000 });
});

// ─── Error de API ─────────────────────────────────────────────────────────────

test('Contraseña actual incorrecta muestra error de API', async ({ page }) => {
    await mockLogin(page);
    await mockPasswordChange(page, 400);

    await page.goto('http://localhost:3000/perfil/contrasena');

    await page.getByTestId('current-password-input').fill('ContraseñaEquivocada!');
    await page.getByTestId('new-password-input').fill('NuevaPassword1!');
    await page.getByTestId('confirm-password-input').fill('NuevaPassword1!');
    await page.getByTestId('submit-password-btn').click();

    await expect(page.getByText(/incorrecta/i)).toBeVisible({ timeout: 10000 });
});

// ─── Navegación ───────────────────────────────────────────────────────────────

test('Enlace Volver al perfil navega a /perfil', async ({ page }) => {
    await mockLogin(page);
    await page.goto('http://localhost:3000/perfil/contrasena');

    await page.getByRole('link', { name: /volver al perfil/i }).click();

    await expect(page).toHaveURL(/\/perfil$/, { timeout: 10000 });
});
