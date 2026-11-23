import type {
  CreatedEvent,
  CreateEventRequest,
  EventDetails,
  EventSummary,
  PagedResponse,
  ProblemDetails,
  Registration,
  RegisterRequest,
  UpdateEventRequest,
} from './types';

/**
 * Thrown for any non-2xx response. `problem` is the RFC 7807 body; business-rule errors carry a
 * stable `code` (e.g. `event_full`) to branch on instead of parsing the message.
 */
export class ApiError extends Error {
  constructor(public readonly problem: ProblemDetails) {
    super(problem.title ?? problem.detail ?? 'Request failed');
  }

  /** Validation errors keyed by camelCase field name. */
  get fieldErrors(): Record<string, string> {
    const result: Record<string, string> = {};
    for (const [key, messages] of Object.entries(this.problem.errors ?? {})) {
      const field = key.charAt(0).toLowerCase() + key.slice(1);
      result[field] = messages.join(' ');
    }
    return result;
  }
}

/** Sends a JSON request and parses the response; resolves to `undefined` for 204 No Content. */
async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: { 'Content-Type': 'application/json', Accept: 'application/json', ...init?.headers },
  });

  if (!response.ok) {
    let problem: ProblemDetails = { status: response.status, title: response.statusText };
    try {
      problem = await response.json();
    } catch {
      // Non-JSON error body; keep the status text.
    }
    throw new ApiError(problem);
  }

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

/**
 * Typed wrappers for every API endpoint. Paths are relative: in development the Vite server proxies
 * `/api` to the backend (see `vite.config.ts`).
 */
export const api = {
  /** Pages are 1-based; the API returns 10 events per page by default. */
  listEvents: (page = 1) => request<PagedResponse<EventSummary>>(`/api/events?page=${page}`),

  getEvent: (eventId: string) => request<EventDetails>(`/api/events/${eventId}`),

  createEvent: (body: CreateEventRequest) =>
    request<CreatedEvent>('/api/events', { method: 'POST', body: JSON.stringify(body) }),

  updateEvent: (eventId: string, body: UpdateEventRequest) =>
    request<EventDetails>(`/api/events/${eventId}`, { method: 'PUT', body: JSON.stringify(body) }),

  deleteEvent: (eventId: string) => request<void>(`/api/events/${eventId}`, { method: 'DELETE' }),

  /** Pages are 1-based; the API returns 10 registrations per page by default. */
  listRegistrations: (eventId: string, page = 1) =>
    request<PagedResponse<Registration>>(`/api/events/${eventId}/registrations?page=${page}`),

  register: (eventId: string, body: RegisterRequest) =>
    request<Registration>(`/api/events/${eventId}/registrations`, {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  unregister: (eventId: string, registrationId: string) =>
    request<void>(`/api/events/${eventId}/registrations/${registrationId}`, { method: 'DELETE' }),
};
