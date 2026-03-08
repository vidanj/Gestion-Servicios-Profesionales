import { test, expect } from '@playwright/test';

// Helper: simula login poniendo token en localStorage
const mockLogin = async (page: any) => {
    await page.goto('http://localhost:3000/usuarios');
    await page.evaluate(() => {
        localStorage.setItem('token', 'fake-jwt-token');
    });
};

// Helper: mockea GET /api/Users con usuarios de prueba
const mockGetUsers = (page: any) => {
    return page.route('**/api/Users**', async (route: any) => {
        if (route.request().method() === 'GET') {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    Data: [
                        {
                            id: 'aaa-111',
                            firstName: 'Juan',
                            lastName: 'Perez',
                            email: 'juan@test.com',
                            phoneNumber: '6641234567',
                            role: 1,
                            status: true,
                        },
                    ],
                    Total: 1,
                    Page: 1,
                    Size: 50,
                }),
            });
        } else {
            await route.continue();
        }
    });
};

// ─────────────────────────────────────────────────────────────
// Carga de página
// ─────────────────────────────────────────────────────────────

test('CRUD page carga correctamente con formulario y tabla', async ({ page }) => {
    await mockGetUsers(page);
    await mockLogin(page);
    await page.reload();

    await expect(page.getByTestId('crud-firstname')).toBeVisible();
    await expect(page.getByTestId('crud-lastname')).toBeVisible();
    await expect(page.getByTestId('crud-email')).toBeVisible();
    await expect(page.getByTestId('crud-phone')).toBeVisible();
    await expect(page.getByTestId('crud-password')).toBeVisible();
    await expect(page.getByTestId('crud-role-button')).toBeVisible();
    await expect(page.getByTestId('crud-create-button')).toBeVisible();
});

test('CRUD page muestra usuarios cargados de la API', async ({ page }) => {
    await mockGetUsers(page);
    await mockLogin(page);
    await page.reload();

    await expect(page.getByTestId('crud-user-row')).toBeVisible();
    await expect(page.getByText('Juan Perez')).toBeVisible();
    await expect(page.getByText('juan@test.com')).toBeVisible();
});

// ─────────────────────────────────────────────────────────────
// Validaciones del formulario
// ─────────────────────────────────────────────────────────────

test('Crear usuario sin nombre muestra validación', async ({ page }) => {
    await mockGetUsers(page);
    await mockLogin(page);
    await page.reload();

    await page.getByTestId('crud-create-button').click();
    await expect(page.getByTestId('crud-error')).toHaveText('El nombre es obligatorio');
});

test('Crear usuario sin apellido muestra validación', async ({ page }) => {
    await mockGetUsers(page);
    await mockLogin(page);
    await page.reload();

    await page.getByTestId('crud-firstname').fill('Juan');
    await page.getByTestId('crud-create-button').click();
    await expect(page.getByTestId('crud-error')).toHaveText('El apellido es obligatorio');
});

test('Crear usuario sin rol muestra validación', async ({ page }) => {
    await mockGetUsers(page);
    await mockLogin(page);
    await page.reload();

    await page.getByTestId('crud-firstname').fill('Juan');
    await page.getByTestId('crud-lastname').fill('Perez');
    await page.getByTestId('crud-create-button').click();
    await expect(page.getByTestId('crud-error')).toHaveText('El rol es obligatorio');
});

test('Crear usuario con email inválido muestra validación', async ({ page }) => {
    await mockGetUsers(page);
    await mockLogin(page);
    await page.reload();

    await page.getByTestId('crud-firstname').fill('Juan');
    await page.getByTestId('crud-lastname').fill('Perez');
    await page.getByTestId('crud-role-button').click();
    await page.getByTestId('crud-role-option-cliente').click();
    await page.getByTestId('crud-email').fill('noesunemail');
    await page.getByTestId('crud-create-button').click();
    await expect(page.getByTestId('crud-error')).toHaveText('El email no es válido');
});

test('Crear usuario sin teléfono muestra validación', async ({ page }) => {
    await mockGetUsers(page);
    await mockLogin(page);
    await page.reload();

    await page.getByTestId('crud-firstname').fill('Juan');
    await page.getByTestId('crud-lastname').fill('Perez');
    await page.getByTestId('crud-role-button').click();
    await page.getByTestId('crud-role-option-cliente').click();
    await page.getByTestId('crud-email').fill('juan@test.com');
    await page.getByTestId('crud-create-button').click();
    await expect(page.getByTestId('crud-error')).toHaveText('El teléfono es obligatorio');
});

test('Crear usuario con contraseña corta muestra validación', async ({ page }) => {
    await mockGetUsers(page);
    await mockLogin(page);
    await page.reload();

    await page.getByTestId('crud-firstname').fill('Juan');
    await page.getByTestId('crud-lastname').fill('Perez');
    await page.getByTestId('crud-role-button').click();
    await page.getByTestId('crud-role-option-cliente').click();
    await page.getByTestId('crud-email').fill('juan@test.com');
    await page.getByTestId('crud-phone').fill('6641234567');
    await page.getByTestId('crud-password').fill('123');
    await page.getByTestId('crud-create-button').click();
    await expect(page.getByTestId('crud-error')).toHaveText('La contraseña debe tener al menos 6 caracteres');
});

// ─────────────────────────────────────────────────────────────
// Crear usuario exitoso
// ─────────────────────────────────────────────────────────────

test('Crear usuario exitoso refresca la tabla', async ({ page }) => {
    await page.route('**/api/Users**', async (route) => {
        if (route.request().method() === 'POST') {
            await route.fulfill({
                status: 201,
                contentType: 'application/json',
                body: JSON.stringify({
                    id: 'bbb-222',
                    firstName: 'Ana',
                    lastName: 'Torres',
                    email: 'ana@test.com',
                    phoneNumber: '6641111111',
                    role: 1,
                    status: true,
                }),
            });
        } else {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ Data: [], Total: 0, Page: 1, Size: 50 }),
            });
        }
    });

    await mockLogin(page);
    await page.reload();

    await page.getByTestId('crud-firstname').fill('Ana');
    await page.getByTestId('crud-lastname').fill('Torres');
    await page.getByTestId('crud-role-button').click();
    await page.getByTestId('crud-role-option-cliente').click();
    await page.getByTestId('crud-email').fill('ana@test.com');
    await page.getByTestId('crud-phone').fill('6641111111');
    await page.getByTestId('crud-password').fill('password123');
    await page.getByTestId('crud-create-button').click();

    // El formulario se limpia tras crear exitosamente
    await expect(page.getByTestId('crud-firstname')).toHaveValue('');
});

// ─────────────────────────────────────────────────────────────
// Eliminar usuario
// ─────────────────────────────────────────────────────────────

test('Eliminar usuario muestra confirmación y ejecuta delete', async ({ page }) => {
    await mockGetUsers(page);
    await page.route('**/api/Users/aaa-111', async (route) => {
        if (route.request().method() === 'DELETE') {
            await route.fulfill({ status: 204 });
        } else {
            await route.continue();
        }
    });

    await mockLogin(page);
    await page.reload();

    await expect(page.getByTestId('crud-user-row')).toBeVisible();
    await page.getByTestId('crud-delete-button').click();

    // Aparece modal de confirmación
    await expect(page.getByTestId('confirm-delete-button')).toBeVisible();
    await page.getByTestId('confirm-delete-button').click();
});

// ─────────────────────────────────────────────────────────────
// Editar usuario
// ─────────────────────────────────────────────────────────────

test('Editar usuario abre modal con datos precargados', async ({ page }) => {
    await mockGetUsers(page);
    await mockLogin(page);
    await page.reload();

    await expect(page.getByTestId('crud-user-row')).toBeVisible();

    // Buscar el botón de editar por aria-label
    await page.getByRole('button', { name: 'Editar' }).first().click();

    await expect(page.getByTestId('edit-firstname')).toBeVisible();
    await expect(page.getByTestId('edit-firstname')).toHaveValue('Juan');
    await expect(page.getByTestId('edit-lastname')).toHaveValue('Perez');
});

test('Editar usuario exitoso cierra el modal', async ({ page }) => {
    await mockGetUsers(page);
    await page.route('**/api/Users/aaa-111', async (route) => {
        if (route.request().method() === 'PUT') {
            await route.fulfill({ status: 204 });
        } else {
            await route.continue();
        }
    });

    await mockLogin(page);
    await page.reload();

    await page.getByRole('button', { name: 'Editar' }).first().click();
    await page.getByTestId('edit-firstname').fill('JuanEditado');
    await page.getByTestId('edit-save-button').click();

    // El modal se cierra
    await expect(page.getByTestId('edit-firstname')).not.toBeVisible();
});

// ─────────────────────────────────────────────────────────────
// Dropdown de rol
// ─────────────────────────────────────────────────────────────

test('Dropdown de rol muestra opciones al hacer click', async ({ page }) => {
    await mockGetUsers(page);
    await mockLogin(page);
    await page.reload();

    await page.getByTestId('crud-role-button').click();
    await expect(page.getByTestId('crud-role-option-cliente')).toBeVisible();
    await expect(page.getByTestId('crud-role-option-profesionista')).toBeVisible();
});

test('Seleccionar rol cliente lo muestra en el botón', async ({ page }) => {
    await mockGetUsers(page);
    await mockLogin(page);
    await page.reload();

    await page.getByTestId('crud-role-button').click();
    await page.getByTestId('crud-role-option-cliente').click();
    await expect(page.getByTestId('crud-role-button')).toContainText('Cliente');
});

// ─────────────────────────────────────────────────────────────
// Modal de edición — validaciones
// ─────────────────────────────────────────────────────────────

test('Editar usuario sin nombre muestra validación', async ({ page }) => {
    await mockGetUsers(page);
    await mockLogin(page);
    await page.reload();

    await page.getByRole('button', { name: 'Editar' }).first().click();
    await page.getByTestId('edit-firstname').clear();
    await page.getByTestId('edit-save-button').click();
    await expect(page.getByText('El nombre es obligatorio')).toBeVisible();
});

test('Editar usuario sin apellido muestra validación', async ({ page }) => {
    await mockGetUsers(page);
    await mockLogin(page);
    await page.reload();

    await page.getByRole('button', { name: 'Editar' }).first().click();
    await page.getByTestId('edit-lastname').clear();
    await page.getByTestId('edit-save-button').click();
    await expect(page.getByText('El apellido es obligatorio')).toBeVisible();
});

// ─────────────────────────────────────────────────────────────
// Cancelar modales
// ─────────────────────────────────────────────────────────────

test('Cancelar modal de edición lo cierra', async ({ page }) => {
    await mockGetUsers(page);
    await mockLogin(page);
    await page.reload();

    await page.getByRole('button', { name: 'Editar' }).first().click();
    await expect(page.getByTestId('edit-firstname')).toBeVisible();
    await page.getByRole('button', { name: 'Cancelar' }).click();
    await expect(page.getByTestId('edit-firstname')).not.toBeVisible();
});

test('Cancelar modal de eliminación lo cierra', async ({ page }) => {
    await mockGetUsers(page);
    await mockLogin(page);
    await page.reload();

    await page.getByTestId('crud-delete-button').click();
    await expect(page.getByTestId('confirm-delete-button')).toBeVisible();
    await page.getByRole('button', { name: 'Cancelar' }).click();
    await expect(page.getByTestId('confirm-delete-button')).not.toBeVisible();
});

// ─────────────────────────────────────────────────────────────
// Navegación
// ─────────────────────────────────────────────────────────────

test('Link Usuarios registrados redirige correctamente', async ({ page }) => {
    await mockGetUsers(page);
    await mockLogin(page);
    await page.reload();

    await page.getByRole('link', { name: 'Usuarios registrados' }).click();
    await expect(page).toHaveURL(/registrados/);
});