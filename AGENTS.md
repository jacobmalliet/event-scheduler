# AGENTS.md

Guidance for AI coding agents (and humans) working in this repository.

## Overview

Event and attendee registration system: an ASP.NET Core REST API with a Vue 3 + TypeScript frontend, orchestrated locally by .NET Aspire. Authentication and authorization are out of scope.

## Layout

```
apphost.cs                          Aspire file-based AppHost (api + web)
EventScheduler.slnx                 Solution (Aspire / src / tests folders)
src/
  EventScheduler.Application/       Domain models (Event, Registration) — no EF or ASP.NET dependencies
  EventScheduler.Infrastructure/    EF Core DbContext, entity configurations, KeyedLock
  EventScheduler.Api/               Minimal API, vertical slices under Features/
  EventScheduler.ServiceDefaults/   Aspire service defaults (OTel, health checks, resilience)
  web/                              Vue 3 + Vite + TypeScript SPA (EventScheduler.Web.esproj)
tests/
  EventScheduler.Api.Tests/             HTTP integration tests (WebApplicationFactory)
  EventScheduler.Application.Tests/     Domain model unit tests
  EventScheduler.Infrastructure.Tests/  Infrastructure unit tests (e.g. KeyedLock)
```

## Commands

```powershell
dotnet build EventScheduler.slnx          # build (code style is enforced, see .editorconfig)
dotnet test EventScheduler.slnx           # run tests
aspire start --apphost apphost.cs         # run api + web in the background
aspire stop                               # stop it
npm run build                             # in src/web: type-check and build the SPA
npm run lint                              # in src/web: ESLint (lint:fix to auto-fix)
npm run format                            # in src/web: Prettier (format:check to verify only)
npm test                                  # in src/web: Vitest browser tests in headless Chromium (first run: npx playwright install chromium)
```

CI (`.github/workflows/ci.yml`) runs the backend build, tests and AppHost build, and the frontend lint, format check and build, on every push to `main` and every pull request. CI sets `CI=true`, which turns every build warning into an error, so keep local builds warning-free.

```powershell
dotnet run --project src/EventScheduler.Api   # API alone on http://localhost:5129 (no Aspire)
npm run dev                                   # in src/web: Vite on :5173, proxies /api to :5129
```

## API and business rules

| Endpoint | Slice | Rules (problem `code`) |
|---|---|---|
| `POST /api/events` | `CreateEvent` | Validation: title 1–200, description ≤ 2000, start in the future, end after start, capacity 1–10,000 |
| `GET /api/events?page=&pageSize=` | `ListEvents` | Paged (`PagedResponse<T>`: `items`, `page`, `pageSize`, `totalCount`, `totalPages`); page 1–`MaxPage` (keeps the offset within `int`), pageSize 1–100, default 10; ordered by `StartsAt` then `Id`; includes `registeredCount` |
| `GET /api/events/{eventId}` | `GetEvent` | 404 `event_not_found` |
| `PUT /api/events/{eventId}` | `UpdateEvent` | Same validation as create; 404 `event_not_found`; 409 `event_in_past` (already started); 409 `capacity_below_registrations` |
| `DELETE /api/events/{eventId}` | `DeleteEvent` | Removes the event and its registrations; 404 `event_not_found`; 409 `event_in_past` |
| `GET /api/events/{eventId}/registrations?page=&pageSize=` | `ListRegistrations` | Paged exactly like `ListEvents` (same limits and `PagedResponse<T>`); ordered by `RegisteredAt` then `Id`; 404 `event_not_found` |
| `POST /api/events/{eventId}/registrations` | `RegisterForEvent` | Email normalized; `Location` is the registration's URL; 404 `event_not_found`; 409 `event_in_past`, `already_registered`, `event_full` |
| `DELETE /api/events/{eventId}/registrations/{registrationId}` | `UnregisterFromEvent` | By registration id (any email works, no URL-encoding issues); 404 `event_not_found`, `registration_not_found`; 409 `event_in_past` |

`RegisterForEvent`, `UpdateEvent`, `UnregisterFromEvent` and `DeleteEvent` all hold the event's `KeyedLock` from their checks through `SaveChanges`. Keep the README's API table in sync when endpoints or rules change.

## Architecture conventions

- **Vertical slices.** One folder per feature, namespace matching the folder (`EventScheduler.Api.Features.<Area>.<Feature>`):

  ```
  Features/
    Events/
      CreateEvent/
        CreateEventEndpoint.cs     // CreateEventRequest + CreateEventResponse records (namespace level, above the class),
                                   // then the IEndpoint class: MapEndpoint + static HandleAsync (+ nested Errors if needed)
        CreateEventValidator.cs    // AbstractValidator<CreateEventRequest>
  ```

  The request/response records live in the endpoint file; the validator gets its own file. Omit the request record and validator when a feature has no input to validate (e.g. `GetEvent`). Query-string input is a request record bound with `[AsParameters]` (see `ListEventsRequest`), so `.WithValidation<TRequest>()` works the same as for bodies. Never expose Application models from endpoints.
- **Paging.** List endpoints that can grow return `PagedResponse<T>` (`Common/PagedResponse.cs`), take a `[AsParameters]` request record defaulting to `Paging.DefaultPageSize`, and validate it against `Paging.MaxPage` / `Paging.MaxPageSize` (`Common/Paging.cs`). They order by a unique key last (e.g. `.ThenBy(e => e.Id)`) so pages never overlap.
- **Endpoint shape.** `MapEndpoint` maps the route to the method group `HandleAsync` and chains metadata (`.WithValidation<TRequest>()`, `.WithName(...)`, `.WithTags(...)`). `internal static async Task<...> HandleAsync(...)` takes the route values, request, services (`EventSchedulerDbContext`, `TimeProvider`, `KeyedLock<Guid>`, ...) and the `CancellationToken` as parameters, each on its own line; minimal APIs inject them.
- Endpoints are registered by convention (`AddSlices` / `MapSlices` in `Common/IEndpoint.cs`); validators by `AddValidatorsFromAssemblyContaining`.
- Attach validation with `.WithValidation<TRequest>()` (runs `ValidationFilter<TRequest>`, returns 400 `ValidationProblem`).
- Business-rule failures: declare them in a nested `internal static class Errors` on the endpoint and return `Errors.X.ToProblem()` (`NotFound` → 404, `Conflict` → 409). Declare each status an endpoint can return with `.ProducesProblem(StatusCodes.Status404NotFound)` etc. in `MapEndpoint`; `ProblemHttpResult` adds no status metadata on its own, so OpenAPI would otherwise omit them. Each error has a stable snake_case `code` the frontend and tests rely on.
- Use `TypedResults` and `Results<...>` return types so OpenAPI metadata is accurate.
- Inject `TimeProvider` for "now"; never call `DateTime.UtcNow` / `DateTimeOffset.UtcNow` in endpoints or validators.
- Normalize emails with `Registration.NormalizeEmail` before comparing or storing.
- Field limits are constants on the domain models (`Event.TitleMaxLength`, `Event.DescriptionMaxLength`, `Event.MaxCapacityLowerBound`/`UpperBound`, `Registration.EmailMaxLength`). Validators and EF configurations use those constants, never literal numbers, and they replace data annotations on the domain models. The frontend's `maxlength`/`min` attributes in `EventForm.vue` are input hints only; update them when a limit changes.

## Persistence

- EF Core **InMemory** provider for now; switching to SQL should only require changing `AddInfrastructure`.
- InMemory does **not** enforce unique indexes or provide transactions. Enforce invariants explicitly in endpoints, and hold `KeyedLock<TKey>` from the rule checks through `SaveChanges` (see `RegisterForEventEndpoint`). Any slice that changes an event's registrations or capacity must take the same per-event lock. `KeyedLock` uses lock striping (a fixed pool of 64 semaphores chosen by key hash), so memory stays bounded, but locks are not reentrant and unrelated keys can share a stripe: never acquire a second key's lock while holding one. The in-memory store is single-process, so this is a complete guarantee; don't add optimistic concurrency tokens or retry loops. Keep the unique `(EventId, Email)` index configured — it takes effect on a relational provider.

## Aspire

- `apphost.cs` is a file-based app. Resolve paths from `AppContext.GetData("EntryPointFileDirectoryPath")`, not `builder.AppHostDirectory` (IDEs like Rider build it from a generated project in their cache folder).
- The Vite dev server proxies `/api` to the API using the `services__api__*` environment variables injected by `WithReference(api)`.

## Dependencies

- Target **.NET 10**. NuGet now serves .NET 11 packages; pin Microsoft packages to `10.0.x` (or the matching `10.x` for `Microsoft.Extensions.*`).
- Don't add FluentAssertions or MediatR (commercial licenses). Use plain xUnit asserts.

## C# style

Formatting and analyzer rules live in `.editorconfig` and are enforced at build time (`Directory.Build.props`). In addition:

- XML doc `<summary>` tags always go on their own lines, even for a single sentence:

  ```csharp
  /// <summary>
  /// A business-rule failure returned by an endpoint.
  /// </summary>
  ```

  Never `/// <summary>Text.</summary>` on one line.
- **Full XML docs** on every C# type and non-private member (including `internal` ones such as `HandleAsync`, `Errors` and EF configurations), with `<param>`, `<typeparam>` and `<returns>` where they apply. Exceptions: API request/response records, and test classes/methods (their names document them; shared test infrastructure such as `ApiTestBase` is documented).
  - Properties start with **"Gets or sets …"** (`{ get; set; }`) or **"Gets …"** (read-only), even when simple: `Gets or sets the unique identifier of the event.`
  - Constructors: `Initializes a new instance of the <see cref="X"/> class.`
  - Interface implementations with nothing to add use `/// <inheritdoc />` (e.g. `MapEndpoint`, `Configure`).
  - To find gaps, temporarily set `dotnet_diagnostic.CS1591.severity = warning` in `.editorconfig` and build; everything flagged except records and tests needs docs. Set it back to `none` afterwards (records and tests would otherwise fail CI).
- Inline comments explain *why*, not *what*.

## Naming (C# and TypeScript)

- No overly short or abbreviated names for variables, parameters, fields or loop variables: `e` reads like `error`, `ct`/`db`/`regs` hide what they hold. Use descriptive names: `createdEvent`, `error`, `cancellationToken`, `dbContext`, `registrations`.
- `event` is a C# keyword: name the specific event instead (`createdEvent`, `listedEvent`) rather than using `@event`. In TypeScript/Vue, `event` is fine.
- **Exception — lambdas:** short parameters are fine in lambda expressions, e.g. `x => x.Id`, `e => e.StartsAt`, `(e) => e.id`.

## Testing

- One test project per production assembly, named `<Assembly>.Tests` (e.g. `EventScheduler.Infrastructure.Tests` tests `EventScheduler.Infrastructure`), referencing only that assembly. Mirror the source folder and namespace: `KeyedLock` in `Concurrency/` is tested by `Concurrency/KeyedLockTests.cs` in `EventScheduler.Infrastructure.Tests.Concurrency`. Add a test project when an assembly gains logic worth testing (ServiceDefaults has none today).
- API tests: one test class per endpoint, mirroring the slice folder, e.g. `Features/Events/UpdateEvent/UpdateEventEndpointTests.cs` in namespace `EventScheduler.Api.Tests.Features.Events.UpdateEvent`. Test classes derive from `ApiTestBase`, which gives every test its own `ApiFactory` (isolated InMemory database via the `Database:Name` setting, plus a `FakeTimeProvider` as `Factory.Clock`) and `Client`, and shared helpers (`CreateEventAsync`, `RegisterAsync`, `UnregisterAsync`, `ListRegistrationsAsync`). Read problem codes with `response.ReadProblemCodeAsync()`.
- Structure every test body with `// Arrange`, `// Act` and `// Assert` comments, in that order, each followed by a blank line before the next section. Act is the single call under test; follow-up reads that verify state (e.g. listing registrations after registering) belong in Assert. Include Arrange even when it only names an input (e.g. `var unknownEventId = Guid.NewGuid();`).
- Cover every business rule through the HTTP API and assert on the problem `code`, not just the status.
- Test methods are named `MethodUnderTest_ConditionUnderTest_ExpectedResult`, each part in PascalCase. `MethodUnderTest` is the slice (feature) name, e.g. `CreateEvent_InvalidPayload_ReturnsValidationProblem` or `RegisterForEvent_EventAtCapacity_ReturnsEventFullConflict`.

## Frontend

- **ESLint** (`eslint.config.ts`: `eslint-plugin-vue` recommended + `@vue/eslint-config-typescript`) checks code quality; **Prettier** (`.prettierrc.json`) owns formatting: semicolons, single quotes, trailing commas, 100-char lines. `@vue/eslint-config-prettier/skip-formatting` disables ESLint's formatting rules so the two never conflict. Don't add ESLint formatting plugins such as `@stylistic`.
- Before finishing frontend changes, run `npm run lint`, `npm run format:check`, `npm run type-check` and `npm test` in `src/web`; all must be clean.
- **Tests:** Vitest browser mode with the Playwright provider (`vitest.config.ts`), so tests run in real headless Chromium; components are rendered with `vitest-browser-vue`.
  - Tests live in `src/web/tests/`, outside `src/`, mirroring its folders (`tests/views/EventsView.test.ts` tests `src/views/EventsView.vue`); `tsconfig.vitest.json` type-checks them. Import app code with the `@/` alias and test helpers with relative paths (`../utilities`).
  - Name tests as plain sentences grouped by `describe` (e.g. `describe('event page')` / `test('shows the business-rule error when the event is full')`), not the C# `Subject_Condition_ExpectedResult` pattern. Keep the `// Arrange`, `// Act`, `// Assert` structure.
  - Page tests (`tests/views/<View>.test.ts`, one file per page) drive the real app and router with `renderApp(path)` and check each page's main behaviour: what it shows, the user actions, and how it reacts to API errors. They test the UI against API responses; the rules themselves are covered by the C# API tests.
  - There's no API in tests. Use the helpers in `tests/utilities.ts`: `fakeApi(routes)` replaces `fetch` with routes keyed `"METHOD /path"`, records requests (`requestsTo(...)`) and answers unexpected ones with a 500; `json`, `problem` and `noContent` build responses; `anEvent`, `aStartedEvent`, `aRegistration` and `aPage` build data. Restore `fetch` with `vi.unstubAllGlobals()` in `afterEach`.
  - `render()` must be awaited; assert on the DOM with `await expect.element(...)`. Tests in one file share a browser page.
  - `vitest.setup.ts` loads the global stylesheet, since tests don't go through `main.ts`; without it pages and failure screenshots are unstyled.
- API calls go through `src/web/src/api/client.ts`; DTO types in `src/web/src/api/types.ts` mirror the API records.
- Shared UI lives in `src/web/src/components` (e.g. `EventForm.vue`, used by both create and edit; `PagerNav.vue`, the Previous/Next controls for any paged list) and date helpers in `src/web/src/utils/dates.ts`. Paged views keep the page in the URL with `usePageQuery()` (`src/web/src/composables`). Event start and end times each use separate `<input type="date">` and `<input type="time">` fields; convert with `toDateAndTimeInputs` / `fromDateAndTimeInputs` (local time to and from UTC ISO), never by hand.
- Layout: pages use the 760px `.container`; a route with `meta: { wide: true }` (typed in `router/route-meta.d.ts`) gets the 1120px container. The events page is side by side (`.events-layout`, sticky form sidebar) and stacks below 860px. Forms adapt to their own width with container queries (`.form` is an `inline-size` container), so the same component works in a sidebar, a full-width card or on a phone.
- The UI may hide actions the API would reject (e.g. no register/edit/unregister once an event has started), but the API is the source of truth for every rule.
- Show server validation errors per field (`ApiError.fieldErrors`) and business-rule errors via the problem `title`.
