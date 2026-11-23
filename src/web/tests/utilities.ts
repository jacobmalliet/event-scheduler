// Shared helpers for the browser tests: a fake API behind `fetch`, data builders and an app renderer.
import { vi } from 'vitest';
import { render } from 'vitest-browser-vue';
import App from '@/App.vue';
import type { EventSummary, PagedResponse, Registration } from '@/api/types';
import router from '@/router';

/** A request the app made to the fake API. */
export interface RecordedRequest {
  method: string;
  /** Path without the query string, e.g. `/api/events`. */
  path: string;
  query: URLSearchParams;
  /** Parsed JSON body, if any. */
  body: unknown;
}

type RouteHandler = (request: RecordedRequest) => Response | Promise<Response>;

/** Routes keyed by `"METHOD /path"` (query strings are ignored when matching). */
export type Routes = Record<string, RouteHandler>;

/**
 * Replaces `fetch` with a fake API that answers from `routes` and records every request.
 * Unexpected requests get a 500 problem naming the request, so a missing route shows up in the UI.
 * Call `vi.unstubAllGlobals()` after each test to restore `fetch`.
 */
export function fakeApi(routes: Routes) {
  const requests: RecordedRequest[] = [];

  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: string, init?: RequestInit) => {
      const url = new URL(input, window.location.origin);
      const request: RecordedRequest = {
        method: init?.method ?? 'GET',
        path: url.pathname,
        query: url.searchParams,
        body: typeof init?.body === 'string' ? JSON.parse(init.body) : undefined,
      };
      requests.push(request);

      const handler = routes[`${request.method} ${request.path}`];
      return handler
        ? handler(request)
        : problem(
            500,
            'unexpected_request',
            `Unexpected request: ${request.method} ${request.path}`,
          );
    }),
  );

  return {
    requests,
    /** The requests made with the given method and path. */
    requestsTo: (method: string, path: string) =>
      requests.filter((request) => request.method === method && request.path === path),
  };
}

export const json = (body: unknown, status = 200) => Response.json(body, { status });

export const noContent = () => new Response(null, { status: 204 });

/** An RFC 7807 problem response, as the API returns for business-rule and validation failures. */
export const problem = (
  status: number,
  code: string,
  title: string,
  errors?: Record<string, string[]>,
) => Response.json({ status, code, title, errors }, { status });

const inDays = (days: number) => new Date(Date.now() + days * 24 * 60 * 60 * 1000).toISOString();

const twoHours = 2 * 60 * 60 * 1000;

/**
 * An event starting in a week, with no registrations, unless overridden. It ends two hours after it
 * starts unless `endsAt` is given, so overriding only `startsAt` still gives a valid event.
 */
export function anEvent(overrides: Partial<EventSummary> = {}): EventSummary {
  const startsAt = overrides.startsAt ?? inDays(7);
  return {
    id: 'event-1',
    title: 'Vue Meetup',
    description: 'Monthly community meetup',
    startsAt,
    endsAt: new Date(new Date(startsAt).getTime() + twoHours).toISOString(),
    maxCapacity: 10,
    registeredCount: 0,
    ...overrides,
  };
}

/** An event that started an hour ago. */
export const aStartedEvent = (overrides: Partial<EventSummary> = {}) =>
  anEvent({ startsAt: new Date(Date.now() - 60 * 60 * 1000).toISOString(), ...overrides });

export const aRegistration = (email: string, id = `registration-${email}`): Registration => ({
  id,
  email,
  registeredAt: inDays(-1),
});

/** One page of results with totals worked out from `totalCount` (defaults to the item count). */
export function aPage<T>(
  items: T[],
  page = 1,
  totalCount = items.length,
  pageSize = 10,
): PagedResponse<T> {
  return {
    items,
    page,
    pageSize,
    totalCount,
    totalPages: totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize),
  };
}

/** Navigates the real router to `path` and renders the whole app, as a user opening that URL. */
export async function renderApp(path: string) {
  await router.push(path);
  await router.isReady();
  return render(App, { global: { plugins: [router] } });
}

/** The path the app is currently showing, including any query string. */
export const currentPath = () => router.currentRoute.value.fullPath;
