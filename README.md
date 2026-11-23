# Event Scheduler

A small event-management app: create, edit and delete events, and register or unregister attendees by email. It's a REST API built with **ASP.NET Core (.NET 10)** plus a **Vue 3 + TypeScript** single-page app, orchestrated locally with **.NET Aspire**.

Business rules enforced by the API:

- You can't register for an event that has already started.
- You can't register beyond an event's maximum capacity.
- The same email can't register twice for the same event (case and surrounding spaces are ignored).

Data is stored **in memory**, so it's lost when the API restarts. See [Design notes](#design-notes).

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0 | |
| [Node.js](https://nodejs.org/) | 22.18+ or 24.12+ | npm comes with it |
| [Aspire CLI](https://aspire.dev) | 13.6+ | Optional: only needed for the one-command setup |

The Aspire dashboard is served over HTTPS. If you've never trusted the .NET development certificate on this machine, run this once:

```powershell
dotnet dev-certs https --trust
```

## Running locally

### Option 1: Aspire (one command)

From the repository root:

```powershell
aspire run
```

Aspire installs the frontend's npm packages, starts the API and the Vue dev server, and prints a link to the **Aspire dashboard**. Open the `web` resource's URL from the dashboard to use the app. The dashboard also shows logs, traces and health for both. Press `Ctrl+C` to stop.

To run in the background instead, use `aspire start --apphost apphost.cs`, and stop it later with `aspire stop`.

### Option 2: Without Aspire (two terminals)

Terminal 1, the API, on http://localhost:5129:

```powershell
dotnet run --project src/EventScheduler.Api
```

Terminal 2, the frontend, on http://localhost:5173:

```powershell
cd src/web
npm ci
npm run dev
```

Open http://localhost:5173. The Vite dev server proxies `/api` requests to the API on port 5129.

## Tests and checks

```powershell
dotnet test EventScheduler.slnx
```

This runs three test projects, one per production assembly:

- `EventScheduler.Api.Tests`: HTTP integration tests for every endpoint and business rule, including concurrent-registration races.
- `EventScheduler.Application.Tests`: unit tests for the business rules on the domain models (event started, event full, capacity below registrations) and email normalization.
- `EventScheduler.Infrastructure.Tests`: unit tests for the per-event lock.

For the frontend, run these in `src/web`:

```powershell
npm run lint
npm run format:check
npm run build
```

`npm run build` type-checks the code as well as building it.

Frontend tests use [Vitest browser mode](https://vitest.dev/guide/browser/) with the Playwright provider, so they run in a real headless Chromium. Install the browser once, then run them from `src/web`:

```powershell
npx playwright install chromium
npm test
```

The [CI workflow](.github/workflows/ci.yml) runs all of the above on every push and pull request. In CI, build warnings, including the `.editorconfig` code-style rules, fail the build.