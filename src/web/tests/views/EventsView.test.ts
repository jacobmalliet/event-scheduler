import { afterEach, describe, expect, test, vi } from 'vitest';
import { userEvent } from 'vitest/browser';
import { aPage, anEvent, currentPath, fakeApi, json, problem, renderApp } from '../utilities';

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('events page', () => {
  test('lists events with their registration counts', async () => {
    // Arrange
    fakeApi({
      'GET /api/events': () =>
        json(aPage([anEvent({ title: 'Vue Meetup', registeredCount: 3, maxCapacity: 10 })])),
    });

    // Act
    const screen = await renderApp('/');

    // Assert
    const row = screen.getByRole('row', { name: /Vue Meetup/ });
    await expect.element(row).toBeVisible();
    await expect.element(row.getByText('3 / 10')).toBeVisible();
    await expect
      .element(screen.getByRole('navigation', { name: 'Event pages' }))
      .not.toBeInTheDocument();
  });

  test('pages through events and keeps the page in the URL', async () => {
    // Arrange
    const api = fakeApi({
      'GET /api/events': ({ query }) => {
        const page = Number(query.get('page'));
        return json(
          aPage([anEvent({ id: `event-${page}`, title: `Event on page ${page}` })], page, 12),
        );
      },
    });
    const screen = await renderApp('/');
    await expect.element(screen.getByText('Page 1 of 2 · 12 events')).toBeVisible();

    // Act
    await screen.getByRole('button', { name: 'Next →' }).click();

    // Assert
    await expect.element(screen.getByText('Event on page 2')).toBeVisible();
    await expect.element(screen.getByText('Page 2 of 2 · 12 events')).toBeVisible();
    await expect.element(screen.getByRole('button', { name: 'Next →' })).toBeDisabled();
    expect(currentPath()).toBe('/?page=2');
    expect(
      api.requestsTo('GET', '/api/events').map((request) => request.query.get('page')),
    ).toEqual(['1', '2']);
  });

  test('creates an event and shows it in the list', async () => {
    // Arrange
    let events = [] as ReturnType<typeof anEvent>[];
    const api = fakeApi({
      'GET /api/events': () => json(aPage(events)),
      'POST /api/events': ({ body }) => {
        const created = anEvent({ id: 'event-new', ...(body as object) });
        events = [created];
        return json(created, 201);
      },
    });
    const screen = await renderApp('/');

    // Act
    await userEvent.fill(screen.getByLabelText('Title'), 'Launch party');
    await userEvent.fill(screen.getByLabelText('Start date'), '2030-06-15');
    await userEvent.fill(screen.getByLabelText('Start time'), '18:30');
    await userEvent.fill(screen.getByLabelText('End date'), '2030-06-15');
    await userEvent.fill(screen.getByLabelText('End time'), '22:00');
    await userEvent.fill(screen.getByLabelText('Max capacity'), '25');
    await screen.getByRole('button', { name: 'Create event' }).click();

    // Assert
    await expect.element(screen.getByRole('link', { name: 'Launch party' })).toBeVisible();
    expect(api.requestsTo('POST', '/api/events')[0]?.body).toEqual({
      title: 'Launch party',
      description: null,
      // The date and time inputs are local time; the API receives UTC timestamps.
      startsAt: new Date('2030-06-15T18:30').toISOString(),
      endsAt: new Date('2030-06-15T22:00').toISOString(),
      maxCapacity: 25,
    });
    // The form is cleared for the next event.
    await expect.element(screen.getByLabelText('Title')).toHaveValue('');
  });

  test('shows the API validation errors next to their fields', async () => {
    // Arrange
    fakeApi({
      'GET /api/events': () => json(aPage([])),
      'POST /api/events': () =>
        problem(400, 'validation', 'One or more validation errors occurred.', {
          Title: ["'Title' must not be empty."],
          StartsAt: ['Event date must be in the future.'],
          EndsAt: ['End time must be after the start time.'],
        }),
    });
    const screen = await renderApp('/');

    // Act
    await userEvent.fill(screen.getByLabelText('Start date'), '2020-01-01');
    await userEvent.fill(screen.getByLabelText('Start time'), '09:00');
    await userEvent.fill(screen.getByLabelText('End date'), '2019-12-31');
    await userEvent.fill(screen.getByLabelText('End time'), '09:00');
    await screen.getByRole('button', { name: 'Create event' }).click();

    // Assert
    await expect.element(screen.getByText("'Title' must not be empty.")).toBeVisible();
    await expect.element(screen.getByText('Event date must be in the future.')).toBeVisible();
    await expect.element(screen.getByText('End time must be after the start time.')).toBeVisible();
  });

  test('requires both a date and a time before calling the API', async () => {
    // Arrange
    const api = fakeApi({ 'GET /api/events': () => json(aPage([])) });
    const screen = await renderApp('/');

    // Act
    await userEvent.fill(screen.getByLabelText('Title'), 'No time yet');
    await userEvent.fill(screen.getByLabelText('Start date'), '2030-06-15');
    await userEvent.fill(screen.getByLabelText('End date'), '2030-06-15');
    await userEvent.fill(screen.getByLabelText('End time'), '22:00');
    await screen.getByRole('button', { name: 'Create event' }).click();

    // Assert
    await expect.element(screen.getByText('Choose both a date and a time.')).toBeVisible();
    expect(api.requestsTo('POST', '/api/events')).toHaveLength(0);
  });
});
