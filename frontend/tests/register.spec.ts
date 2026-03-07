import { test, expect } from '@playwright/test';

test('Register page carga correctamente', async ({ page }) => {
    await page.goto('http://localhost:3000/register');

    await expect(page.getByTestId('firstname-input')).toBeVisible();
    await expect(page.getByTestId('lastname-input')).toBeVisible();
    await expect(page.getByTestId('username-input')).toBeVisible();
    await expect(page.getByTestId('phone-input')).toBeVisible();
    await expect(page.getByTestId('password-input')).toBeVisible();
    await expect(page.getByTestId('register-button')).toBeVisible();
    await expect(page.getByTestId('role-button')).toBeVisible();
});

test('Registro exitoso redirige al login', async ({ page }) => {
    await page.route('**/api/Auth/register', async route => {
        await route.fulfill({
            status: 201,
            contentType: 'application/json',
            body: JSON.stringify({ token: 'fake-jwt-token' })
        });
    });

    await page.goto('http://localhost:3000/register');
    await page.getByTestId('firstname-input').fill('Juan');
    await page.getByTestId('lastname-input').fill('Perez');
    await page.getByTestId('username-input').fill('juan@ejemplo.com');
    await page.getByTestId('role-button').click();
    await page.getByTestId('role-option-cliente').click();
    await page.getByTestId('phone-input').fill('6641234567');
    await page.getByTestId('password-input').fill('password123');
    await page.getByTestId('register-button').click();

    await expect(page).toHaveURL(/login/, { timeout: 10000 });
});

test('Registro con nombre vacío muestra validación', async ({ page }) => {
    await page.goto('http://localhost:3000/register');
    await page.getByTestId('register-button').click();
    await expect(page.getByTestId('error-message')).toHaveText('El nombre es obligatorio');
});

test('Registro con apellido vacío muestra validación', async ({ page }) => {
    await page.goto('http://localhost:3000/register');
    await page.getByTestId('firstname-input').fill('Juan');
    await page.getByTestId('register-button').click();
    await expect(page.getByTestId('error-message')).toHaveText('El apellido es obligatorio');
});

test('Registro con email vacío muestra validación', async ({ page }) => {
    await page.goto('http://localhost:3000/register');
    await page.getByTestId('firstname-input').fill('Juan');
    await page.getByTestId('lastname-input').fill('Perez');
    await page.getByTestId('role-button').click();
    await page.getByTestId('role-option-cliente').click();
    await page.getByTestId('register-button').click();
    await expect(page.getByTestId('error-message')).toHaveText('El email es obligatorio');
});

test('Registro con email inválido muestra validación', async ({ page }) => {
    await page.goto('http://localhost:3000/register');
    await page.getByTestId('firstname-input').fill('Juan');
    await page.getByTestId('lastname-input').fill('Perez');
    await page.getByTestId('role-button').click();
    await page.getByTestId('role-option-cliente').click();
    await page.getByTestId('username-input').fill('noesunemail');
    await page.getByTestId('register-button').click();
    await expect(page.getByTestId('error-message')).toHaveText('El email no es válido');
});

test('Registro con teléfono vacío muestra validación', async ({ page }) => {
    await page.goto('http://localhost:3000/register');
    await page.getByTestId('firstname-input').fill('Juan');
    await page.getByTestId('lastname-input').fill('Perez');
    await page.getByTestId('role-button').click();
    await page.getByTestId('role-option-cliente').click();
    await page.getByTestId('username-input').fill('juan@ejemplo.com');
    await page.getByTestId('register-button').click();
    await expect(page.getByTestId('error-message')).toHaveText('El teléfono es obligatorio');
});

test('Registro con contraseña vacía muestra validación', async ({ page }) => {
    await page.goto('http://localhost:3000/register');
    await page.getByTestId('firstname-input').fill('Juan');
    await page.getByTestId('lastname-input').fill('Perez');
    await page.getByTestId('role-button').click();
    await page.getByTestId('role-option-cliente').click();
    await page.getByTestId('username-input').fill('juan@ejemplo.com');
    await page.getByTestId('phone-input').fill('6641234567');
    await page.getByTestId('register-button').click();
    await expect(page.getByTestId('error-message')).toHaveText('La contraseña es obligatoria');
});

test('Registro con contraseña corta muestra validación', async ({ page }) => {
    await page.goto('http://localhost:3000/register');
    await page.getByTestId('firstname-input').fill('Juan');
    await page.getByTestId('lastname-input').fill('Perez');
    await page.getByTestId('role-button').click();
    await page.getByTestId('role-option-cliente').click();
    await page.getByTestId('username-input').fill('juan@ejemplo.com');
    await page.getByTestId('phone-input').fill('6641234567');
    await page.getByTestId('password-input').fill('123');
    await page.getByTestId('register-button').click();
    await expect(page.getByTestId('error-message')).toHaveText('La contraseña debe tener al menos 6 caracteres');
});

test('Link ¿Ya tienes cuenta? redirige al login', async ({ page }) => {
    await page.goto('http://localhost:3000/register');
    await page.getByText('¿Ya tienes cuenta?').click();
    await expect(page).toHaveURL(/login/);
});