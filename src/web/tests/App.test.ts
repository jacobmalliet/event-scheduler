import { afterEach, describe, expect, test, vi } from 'vitest';
import { aPage, fakeApi, json, renderApp } from './utilities';

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('app', () => {
  test('loads the events page with the form and an empty list', async () => {
    // Arrange
    const api = fakeApi({ 'GET /api/events': () => json(aPage([])) });

    // Act
    const screen = await renderApp('/');

    // Assert
    await expect.element(screen.getByRole('link', { name: 'Event Scheduler' })).toBeVisible();
    await expect.element(screen.getByRole('heading', { name: 'New event' })).toBeVisible();
    await expect.element(screen.getByRole('button', { name: 'Create event' })).toBeVisible();
    await expect.element(screen.getByText('No events yet.', { exact: false })).toBeVisible();
    expect(api.requestsTo('GET', '/api/events')[0]?.query.get('page')).toBe('1');
  });
});
