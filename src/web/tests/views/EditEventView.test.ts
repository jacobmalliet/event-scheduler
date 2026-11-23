import { afterEach, describe, expect, test, vi } from 'vitest';
import { userEvent } from 'vitest/browser';
import {
  aPage,
  aStartedEvent,
  anEvent,
  currentPath,
  fakeApi,
  json,
  problem,
  renderApp,
} from '../utilities';

const editPath = '/events/event-1/edit';
const startsAt = new Date('2030-06-15T18:30').toISOString();
const endsAt = new Date('2030-06-16T01:00').toISOString();

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('edit event page', () => {
  test('fills the form with the event as it is now', async () => {
    // Arrange
    fakeApi({
      'GET /api/events/event-1': () =>
        json(
          anEvent({
            title: 'Vue Meetup',
            description: 'Monthly',
            startsAt,
            endsAt,
            maxCapacity: 40,
          }),
        ),
    });

    // Act
    const screen = await renderApp(editPath);

    // Assert
    await expect.element(screen.getByLabelText('Title')).toHaveValue('Vue Meetup');
    await expect.element(screen.getByLabelText('Description')).toHaveValue('Monthly');
    // Shown in local time, the same moment the API stored in UTC.
    await expect.element(screen.getByLabelText('Start date')).toHaveValue('2030-06-15');
    await expect.element(screen.getByLabelText('Start time')).toHaveValue('18:30');
    await expect.element(screen.getByLabelText('End date')).toHaveValue('2030-06-16');
    await expect.element(screen.getByLabelText('End time')).toHaveValue('01:00');
    await expect.element(screen.getByLabelText('Max capacity')).toHaveValue(40);
  });

  test('saves the changes and returns to the event page', async () => {
    // Arrange
    const api = fakeApi({
      'GET /api/events/event-1': () => json(anEvent({ title: 'Vue Meetup', startsAt, endsAt })),
      'PUT /api/events/event-1': ({ body }) => json({ ...anEvent(), ...(body as object) }),
      'GET /api/events/event-1/registrations': () => json(aPage([])),
    });
    const screen = await renderApp(editPath);
    await expect.element(screen.getByLabelText('Title')).toHaveValue('Vue Meetup');

    // Act
    await userEvent.fill(screen.getByLabelText('Title'), 'Vue Meetup (June)');
    await userEvent.fill(screen.getByLabelText('Max capacity'), '50');
    await screen.getByRole('button', { name: 'Save changes' }).click();

    // Assert
    await expect.element(screen.getByRole('heading', { name: 'Attendees (0)' })).toBeVisible();
    expect(currentPath()).toBe('/events/event-1/registrations');
    expect(api.requestsTo('PUT', '/api/events/event-1')[0]?.body).toEqual({
      title: 'Vue Meetup (June)',
      description: 'Monthly community meetup',
      startsAt,
      endsAt,
      maxCapacity: 50,
    });
  });

  test('shows the business-rule error when capacity would drop below the registrations', async () => {
    // Arrange
    fakeApi({
      'GET /api/events/event-1': () => json(anEvent({ startsAt, registeredCount: 5 })),
      'PUT /api/events/event-1': () =>
        problem(
          409,
          'capacity_below_registrations',
          'Max capacity cannot be lower than the number of current registrations.',
        ),
    });
    const screen = await renderApp(editPath);
    await expect
      .element(screen.getByText("5 registered, so max capacity can't go below that."))
      .toBeVisible();

    // Act
    await userEvent.fill(screen.getByLabelText('Max capacity'), '2');
    await screen.getByRole('button', { name: 'Save changes' }).click();

    // Assert
    await expect
      .element(
        screen.getByText('Max capacity cannot be lower than the number of current registrations.'),
      )
      .toBeVisible();
    expect(currentPath()).toBe(editPath);
  });

  test('asks for a whole-number capacity before calling the API', async () => {
    // Arrange
    const api = fakeApi({ 'GET /api/events/event-1': () => json(anEvent({ startsAt })) });
    const screen = await renderApp(editPath);
    await expect.element(screen.getByLabelText('Max capacity')).toBeVisible();

    // Act
    await userEvent.fill(screen.getByLabelText('Max capacity'), '2.5');
    await screen.getByRole('button', { name: 'Save changes' }).click();

    // Assert
    await expect.element(screen.getByText('Enter a whole number.')).toBeVisible();
    expect(api.requestsTo('PUT', '/api/events/event-1')).toHaveLength(0);
  });

  test('refuses to edit an event that has already started', async () => {
    // Arrange
    fakeApi({ 'GET /api/events/event-1': () => json(aStartedEvent()) });

    // Act
    const screen = await renderApp(editPath);

    // Assert
    await expect
      .element(screen.getByText('This event has started, so it can no longer be edited.'))
      .toBeVisible();
    await expect
      .element(screen.getByRole('button', { name: 'Save changes' }))
      .not.toBeInTheDocument();
  });
});
