// Mirrors the API's request/response records. Timestamps are ISO 8601 strings in UTC.

/** ListEventsResponse, GetEventResponse and UpdateEventResponse share this shape. */
export interface EventSummary {
  id: string;
  title: string;
  description: string | null;
  startsAt: string;
  endsAt: string;
  maxCapacity: number;
  registeredCount: number;
}

export type EventDetails = EventSummary;

/** PagedResponse<T>: one page of results plus totals for paging controls. */
export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/** Body of both CreateEventRequest and UpdateEventRequest. */
export interface EventInput {
  title: string;
  description: string | null;
  startsAt: string;
  endsAt: string;
  maxCapacity: number;
}

export type CreateEventRequest = EventInput;
export type UpdateEventRequest = EventInput;

export interface CreatedEvent {
  id: string;
  title: string;
  description: string | null;
  startsAt: string;
  endsAt: string;
  maxCapacity: number;
}

export interface Registration {
  id: string;
  email: string;
  registeredAt: string;
}

export interface RegisterRequest {
  email: string;
}

/** RFC 7807 problem details, including ASP.NET Core validation errors. */
export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
}
