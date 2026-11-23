/** Formats an ISO timestamp for display in the user's locale, e.g. "Dec 15, 2026, 6:00 PM". */
export const formatDate = (iso: string) =>
  new Date(iso).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });

/**
 * Formats a start and end timestamp as one range in the user's locale, leaving out the repeated date when
 * both fall on the same day, e.g. "Dec 15, 2026, 6:00 – 9:00 PM".
 */
export const formatDateRange = (startIso: string, endIso: string) =>
  new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).formatRange(
    new Date(startIso),
    new Date(endIso),
  );

const pad = (value: number) => String(value).padStart(2, '0');

/**
 * Splits an ISO timestamp into the local-time values `<input type="date">` ("YYYY-MM-DD") and
 * `<input type="time">` ("HH:mm") expect. Those inputs have no time zone, so the user's own is used.
 */
export function toDateAndTimeInputs(iso: string): { date: string; time: string } {
  const value = new Date(iso);
  return {
    date: `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}`,
    time: `${pad(value.getHours())}:${pad(value.getMinutes())}`,
  };
}

/**
 * Combines date and time input values (the user's local time) into an ISO timestamp in UTC,
 * or returns null if either is missing.
 */
export const fromDateAndTimeInputs = (date: string, time: string): string | null =>
  date && time ? new Date(`${date}T${time}`).toISOString() : null;

/** True once `iso` is in the past by the browser's clock. Only used to hide actions; the API decides. */
export const hasStarted = (iso: string) => new Date(iso).getTime() <= Date.now();
