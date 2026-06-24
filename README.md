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

This is the frontend only. It talks to the **PaperlessREST** API (ASP.NET Core),
proxied in development via `proxy.conf.json`:

| path | target |
|---|---|
| `/api/*` | `http://localhost:5057` (documents, search, SSE) |
| `/hangfire` | `http://localhost:5057` (batch-job dashboard) |

Bring up the API and its infrastructure (Postgres, RabbitMQ, MinIO, Elasticsearch)
plus the OCR worker before running this app.

## Develop

```bash
pnpm install --frozen-lockfile
pnpm start     # ng serve → http://localhost:4200
pnpm build     # production build
pnpm e2e       # Playwright end-to-end tests (needs the dev server + backend running)
```

> The `generate:openapi` / `generate:types` scripts expect a sibling `../PaperlessREST`
> checkout. Standalone, the app uses the hand-typed contract in
> `src/app/features/documents/data/document.models.ts`.
