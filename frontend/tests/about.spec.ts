import { test, expect } from '@playwright/test';

test('About page carga correctamente', async ({ page }) => {
    await page.goto('http://localhost:3000/about');
    await expect(page.getByTestId('about-title')).toBeVisible();
});

test('About page muestra el título correcto', async ({ page }) => {
    await page.goto('http://localhost:3000/about');
    await expect(page.getByTestId('about-title')).toHaveText('About Our Marketplace');
});

test('About page muestra la descripción', async ({ page }) => {
    await page.goto('http://localhost:3000/about');
    await expect(page.getByTestId('about-description')).toBeVisible();
});

test('About page muestra las 3 estadísticas', async ({ page }) => {
    await page.goto('http://localhost:3000/about');
    await expect(page.getByTestId('stat-freelancers')).toBeVisible();
    await expect(page.getByTestId('stat-projects')).toBeVisible();
    await expect(page.getByTestId('stat-clients')).toBeVisible();
});

test('About page muestra valores correctos en estadísticas', async ({ page }) => {
    await page.goto('http://localhost:3000/about');
    await expect(page.getByTestId('stat-freelancers')).toContainText('12K+');
    await expect(page.getByTestId('stat-projects')).toContainText('48K+');
    await expect(page.getByTestId('stat-clients')).toContainText('9K+');
});

test('About page muestra la misión', async ({ page }) => {
    await page.goto('http://localhost:3000/about');
    await expect(page.getByTestId('about-mission')).toBeVisible();
});

test('CTA redirige al login', async ({ page }) => {
    await page.goto('http://localhost:3000/about');
    await page.getByTestId('about-cta').click();
    await expect(page).toHaveURL(/login/);
});