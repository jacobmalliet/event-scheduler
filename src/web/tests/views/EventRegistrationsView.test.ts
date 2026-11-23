import { afterEach, describe, expect, test, vi } from 'vitest';
import { userEvent } from 'vitest/browser';
import {
  aPage,
  aRegistration,
  aStartedEvent,
  anEvent,
  currentPath,
  fakeApi,
  json,
  noContent,
  problem,
  renderApp,
} from '../utilities';
import type { Routes } from '../utilities';
import type { EventSummary, Registration } from '@/api/types';

const eventPath = '/events/event-1/registrations';

/** Server-side state the fake API reads, which a test's extra routes can change. */
interface EventState {
  event: EventSummary;
  attendees: Registration[];
}

/** A fake API for one event and its attendees, plus any extra routes a test needs. */
function fakeEventApi(
  event: EventSummary,
  attendees: Registration[],
  extraRoutes: (state: EventState) => Routes = () => ({}),
) {
  const state: EventState = { event, attendees };
  const api = fakeApi({
    'GET /api/events/event-1': () =>
      json({ ...state.event, registeredCount: state.attendees.length }),
    'GET /api/events/event-1/registrations': ({ query }) => {
      const page = Number(query.get('page'));
      const pageItems = state.attendees.slice((page - 1) * 10, page * 10);
      return json(aPage(pageItems, page, state.attendees.length));
    },
    ...extraRoutes(state),
  });
  return { api, state };
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('event page', () => {
  test('shows the event, the spots left and its attendees', async () => {
    // Arrange
    fakeEventApi(anEvent({ title: 'Vue Meetup', maxCapacity: 10 }), [
      aRegistration('ada@example.com'),
      aRegistration('grace@example.com'),
    ]);

    // Act
    const screen = await renderApp(eventPath);

    // Assert
    await expect.element(screen.getByRole('heading', { name: 'Vue Meetup' })).toBeVisible();
    await expect.element(screen.getByText(/8 of 10 spots left/)).toBeVisible();
    await expect.element(screen.getByRole('heading', { name: 'Attendees (2)' })).toBeVisible();
    await expect.element(screen.getByRole('cell', { name: 'ada@example.com' })).toBeVisible();
    await expect.element(screen.getByRole('cell', { name: 'grace@example.com' })).toBeVisible();
  });

  test('registers an attendee and refreshes the list', async () => {
    // Arrange
    fakeEventApi(anEvent(), [], (state) => ({
      'POST /api/events/event-1/registrations': ({ body }) => {
        const registration = aRegistration((body as { email: string }).email);
        state.attendees = [...state.attendees, registration];
        return json(registration, 201);
      },
    }));
    const screen = await renderApp(eventPath);

    // Act
    await userEvent.fill(screen.getByLabelText('Email address'), 'ada@example.com');
    await screen.getByRole('button', { name: 'Register' }).click();

    // Assert
    await expect.element(screen.getByRole('cell', { name: 'ada@example.com' })).toBeVisible();
    await expect.element(screen.getByRole('heading', { name: 'Attendees (1)' })).toBeVisible();
    await expect.element(screen.getByLabelText('Email address')).toHaveValue('');
  });

  test('shows the business-rule error when the event is full', async () => {
    // Arrange
    fakeEventApi(anEvent({ maxCapacity: 1 }), [aRegistration('first@example.com')], () => ({
      'POST /api/events/event-1/registrations': () =>
        problem(409, 'event_full', 'Event is at capacity.'),
    }));
    const screen = await renderApp(eventPath);

    // Act
    await userEvent.fill(screen.getByLabelText('Email address'), 'second@example.com');
    await screen.getByRole('button', { name: 'Register' }).click();

    // Assert
    await expect.element(screen.getByText('Event is at capacity.')).toBeVisible();
    await expect.element(screen.getByRole('heading', { name: 'Attendees (1)' })).toBeVisible();
  });

  test('unregisters an attendee by registration id', async () => {
    // Arrange
    fakeEventApi(
      anEvent(),
      [
        aRegistration('ada@example.com', 'registration-ada'),
        aRegistration('grace@example.com', 'registration-grace'),
      ],
      (state) => ({
        'DELETE /api/events/event-1/registrations/registration-ada': () => {
          state.attendees = state.attendees.filter(
            (attendee) => attendee.id !== 'registration-ada',
          );
          return noContent();
        },
      }),
    );
    const screen = await renderApp(eventPath);

    // Act
    await screen
      .getByRole('row', { name: /ada@example.com/ })
      .getByRole('button', { name: 'Unregister' })
      .click();

    // Assert
    await expect
      .element(screen.getByRole('cell', { name: 'ada@example.com' }))
      .not.toBeInTheDocument();
    await expect.element(screen.getByRole('cell', { name: 'grace@example.com' })).toBeVisible();
    await expect.element(screen.getByRole('heading', { name: 'Attendees (1)' })).toBeVisible();
  });

  test('closes registration and hides every action once the event has started', async () => {
    // Arrange
    fakeEventApi(aStartedEvent(), [aRegistration('ada@example.com')]);

    // Act
    const screen = await renderApp(eventPath);

    // Assert
    await expect
      .element(screen.getByText('This event has started; registrations are closed.'))
      .toBeVisible();
    await expect.element(screen.getByRole('cell', { name: 'ada@example.com' })).toBeVisible();
    await expect.element(screen.getByLabelText('Email address')).not.toBeInTheDocument();
    await expect.element(screen.getByRole('link', { name: 'Edit' })).not.toBeInTheDocument();
    await expect.element(screen.getByRole('button', { name: 'Delete' })).not.toBeInTheDocument();
    await expect
      .element(screen.getByRole('button', { name: 'Unregister' }))
      .not.toBeInTheDocument();
  });

  test('deletes the event after confirmation and returns to the list', async () => {
    // Arrange
    const { api } = fakeEventApi(
      anEvent({ title: 'Vue Meetup' }),
      [aRegistration('ada@example.com')],
      () => ({
        'DELETE /api/events/event-1': () => noContent(),
        'GET /api/events': () => json(aPage([])),
      }),
    );
    const confirm = vi.fn(() => true);
    vi.stubGlobal('confirm', confirm);
    const screen = await renderApp(eventPath);

    // Act
    await screen.getByRole('button', { name: 'Delete' }).click();

    // Assert
    await expect.element(screen.getByRole('heading', { name: 'New event' })).toBeVisible();
    expect(currentPath()).toBe('/');
    expect(confirm).toHaveBeenCalledWith(
      'Delete "Vue Meetup"? Its 1 registration will be removed too.',
    );
    expect(api.requestsTo('DELETE', '/api/events/event-1')).toHaveLength(1);
  });

  test('keeps the event when deletion is cancelled', async () => {
    // Arrange
    const { api } = fakeEventApi(anEvent({ title: 'Vue Meetup' }), []);
    vi.stubGlobal(
      'confirm',
      vi.fn(() => false),
    );
    const screen = await renderApp(eventPath);

    // Act
    await screen.getByRole('button', { name: 'Delete' }).click();

    // Assert
    await expect.element(screen.getByRole('heading', { name: 'Vue Meetup' })).toBeVisible();
    expect(currentPath()).toBe(eventPath);
    expect(api.requestsTo('DELETE', '/api/events/event-1')).toHaveLength(0);
  });

  test('pages through attendees', async () => {
    // Arrange
    const attendees = Array.from({ length: 12 }, (_, index) =>
      aRegistration(`attendee${String(index + 1).padStart(2, '0')}@example.com`),
    );
    fakeEventApi(anEvent({ maxCapacity: 20 }), attendees);
    const screen = await renderApp(eventPath);
    await expect.element(screen.getByText('Page 1 of 2 · 12 attendees')).toBeVisible();

    // Act
    await screen.getByRole('button', { name: 'Next →' }).click();

    // Assert
    await expect
      .element(screen.getByRole('cell', { name: 'attendee11@example.com' }))
      .toBeVisible();
    await expect
      .element(screen.getByRole('cell', { name: 'attendee01@example.com' }))
      .not.toBeInTheDocument();
    expect(currentPath()).toBe(`${eventPath}?page=2`);
  });
});
