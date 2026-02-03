/**
 * E2E tests for the Release Retention Console.
 */

import { test, expect } from '@playwright/test';

test.describe('Release Retention Console', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('shows app title', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Release Retention Console');
  });

  test('shows dataset editor', async ({ page }) => {
    await expect(page.getByLabel(/Paste or upload dataset JSON/)).toBeVisible();
  });

  test('shows validation placeholder initially', async ({ page }) => {
    await expect(page.getByText(/Paste or upload a dataset to see validation/)).toBeVisible();
  });

  test('shows evaluation placeholder initially', async ({ page }) => {
    await expect(page.getByText(/Validate a dataset and click "Evaluate"/)).toBeVisible();
  });

  test('validates dataset on paste', async ({ page }) => {
    const validDataset = JSON.stringify({
      projects: [{ id: 'P1', name: 'Project 1' }],
      environments: [{ id: 'E1', name: 'Production' }],
      releases: [
        { id: 'R1', projectId: 'P1', version: '1.0', created: '2024-01-01T00:00:00Z' },
      ],
      deployments: [
        { id: 'D1', releaseId: 'R1', environmentId: 'E1', deployedAt: '2024-01-01T12:00:00Z' },
      ],
    }, null, 2);

    const editor = page.getByLabel(/Paste or upload dataset JSON/);
    await editor.fill(validDataset);

    // Client-side validation runs automatically
    await expect(page.getByRole('status')).toContainText('✓ Valid');
    await expect(page.getByText('Projects: 1')).toBeVisible();
    await expect(page.getByText('Environments: 1')).toBeVisible();
    await expect(page.getByText('Releases: 1')).toBeVisible();
    await expect(page.getByText('Deployments: 1')).toBeVisible();
  });

  test('shows error for invalid JSON', async ({ page }) => {
    const editor = page.getByLabel(/Paste or upload dataset JSON/);
    await editor.fill('{ invalid json');

    await expect(page.getByRole('alert')).toContainText(/JSON parse error/);
  });

  test('shows error for missing required fields', async ({ page }) => {
    const invalidDataset = JSON.stringify({
      projects: [{ id: '', name: 'Missing ID' }],
      environments: [],
      releases: [],
      deployments: [],
    }, null, 2);

    const editor = page.getByLabel(/Paste or upload dataset JSON/);
    await editor.fill(invalidDataset);

    await expect(page.getByRole('status')).toContainText('✗ Invalid');
    await expect(page.getByText('Errors (1)')).toBeVisible();
    await expect(page.getByText('validation.missing_required_field')).toBeVisible();
  });

  test('releasesToKeep input increments and decrements', async ({ page }) => {
    const input = page.getByRole('textbox', { name: /releases-to-keep/i });
    await expect(input).toHaveValue('1');

    await page.getByLabel('Increase value').click();
    await expect(input).toHaveValue('2');

    await page.getByLabel('Decrease value').click();
    await expect(input).toHaveValue('1');
  });

  test('evaluate button is disabled without valid dataset', async ({ page }) => {
    const evaluateButton = page.getByRole('button', { name: /Evaluate/ });
    await expect(evaluateButton).toBeDisabled();
  });

  test('evaluate button is enabled with valid dataset', async ({ page }) => {
    const validDataset = JSON.stringify({
      projects: [{ id: 'P1', name: 'Project 1' }],
      environments: [{ id: 'E1', name: 'Production' }],
      releases: [
        { id: 'R1', projectId: 'P1', version: '1.0', created: '2024-01-01T00:00:00Z' },
      ],
      deployments: [
        { id: 'D1', releaseId: 'R1', environmentId: 'E1', deployedAt: '2024-01-01T12:00:00Z' },
      ],
    }, null, 2);

    const editor = page.getByLabel(/Paste or upload dataset JSON/);
    await editor.fill(validDataset);

    // Wait for validation
    await expect(page.getByRole('status')).toContainText('✓ Valid');

    const evaluateButton = page.getByRole('button', { name: /Evaluate/ });
    await expect(evaluateButton).toBeEnabled();
  });

  test('export button is disabled without evaluation results', async ({ page }) => {
    const exportButton = page.getByRole('button', { name: /Export Results/ });
    await expect(exportButton).toBeDisabled();
  });
});
