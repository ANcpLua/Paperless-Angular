# Paperless-Angular

[![Tests and Coverage](https://github.com/ANcpLua/Paperless-Angular/actions/workflows/coverage.yml/badge.svg?branch=main)](https://github.com/ANcpLua/Paperless-Angular/actions/workflows/coverage.yml)
[![codecov](https://codecov.io/gh/ANcpLua/Paperless-Angular/branch/main/graph/badge.svg)](https://codecov.io/gh/ANcpLua/Paperless-Angular)

Angular 21 frontend for the **Paperless** document-OCR system — a 1:1, fully transpiled
re-implementation of the reference vanilla-JS SPA, built with standalone components,
signals, and OnPush change detection.

## Features

- Drag-and-drop PDF upload (PDF-only, with rejection warnings)
- Document grid with status badges (Pending / Completed / Failed), a Pending spinner,
  an OCR text preview (capped at 500 chars), and an AI-summary section
- Delete with confirmation
- Full-text search + clear + refresh
- Light / dark theme toggle (persisted to `localStorage`)
- Auto-dismissing toast notifications
- Live updates over two Server-Sent-Event streams (OCR + GenAI) with auto-reconnect
  and a "disconnected" indicator

## Stack

Angular 21 · TypeScript · RxJS · Bootstrap 5.3 · Vite · Playwright (e2e) · pnpm 10.

## Backend

The Angular app talks to the **PaperlessREST** API (ASP.NET Core), proxied in development
via `proxy.conf.json`:

| path | target |
|---|---|
| `/api/*` | `http://localhost:5057` (documents, search, SSE) |
| `/hangfire` | `http://localhost:5057` (batch-job dashboard) |

A copy of the full .NET backend stack is bundled in [`backend/`](backend/): `PaperlessREST`
(API + SSE), `PaperlessServices` (OCR + GenAI worker), shared test support, the NUKE
`Pipeline`, `compose.yaml`, and the central MSBuild infra (`global.json`,
`Directory.Packages.props`, `Version.props`, `nuget.config`). The canonical home of the
backend is the [`ANcpLua/Paperless`](https://github.com/ANcpLua/Paperless) monorepo (which
will eventually carry every framework's UI); this repo mirrors it so the Angular frontend
and its server build side-by-side.

```bash
cd backend
docker compose up -d                    # postgres, rabbitmq, minio, elasticsearch
dotnet build Paperless.slnx             # or ./build.sh Compile (NUKE)
dotnet run --project PaperlessREST      # API on http://localhost:5057
```

## Develop

```bash
pnpm install --frozen-lockfile
pnpm start     # ng serve → http://localhost:4200
pnpm build     # production build
pnpm e2e       # Playwright end-to-end tests (needs the dev server + backend running)
```

> **DTO contract.** The wire types in `src/app/core/api/generated/api-types.ts` mirror the
> C# transport records in `backend/PaperlessREST` field-for-field (the source of truth);
> `src/app/features/documents/data/document.models.ts` layers the app-facing `DocumentStatus`
> union, the normalized `DocumentSummary` view model, and `toSummary` on top, with
> compile-time conformance guards. The `generate:openapi` / `generate:types` scripts target
> the bundled `backend/PaperlessREST` — once the backend gains build-time OpenAPI emit
> (`Microsoft.Extensions.ApiDescription.Server`), `pnpm generate` will regenerate
> `api-types.ts` directly from the emitted `openapi/paperless.json`.
