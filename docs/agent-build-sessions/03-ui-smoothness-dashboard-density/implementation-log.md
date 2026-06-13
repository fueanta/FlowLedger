# Implementation Log

## Session start

### Planned

- Improve app navigation/modal smoothness without adding animation libraries.
- Add first-load Work Queue badge with reload-only reset.
- Enrich non-production demo dashboard data across 1, 3, 6, and 12 month windows.
- Add focused backend/frontend tests and runtime smoke verification.
- Keep README and flow image reader-facing, without explicit session number labels.

### Discovery

- Current app already has `MyWorkQueuePage`, `WorkQueueController`, `WorkQueueService`, and Session 02 workflow-admin/audit additions.
- Existing non-production `DemoSeedDataRefresher` refreshes BR-2026 demo rows to recent dates.
- Dashboard period filters already exist, but existing refreshed rows are too concentrated in the default 1-month window to create meaningful 1/3/6/12 month variation.
- UI/UX search guidance: use short 150-300ms transitions, avoid excessive motion, use ease-out/ease-in, and respect `prefers-reduced-motion`.

### Notes

- Per project instruction, changes are not committed until explicit user signal.

## Completed work

### UI smoothness

- Added reusable route-content transition wrapper in app layout with subtle fade and `translateY` enter motion.
- Updated shared dialog primitive to animate overlay opacity and panel scale/translate on open and close.
- Added reduced-motion CSS handling so route and dialog motion disable for motion-sensitive users.
- Added one-second minimum frontend delay for `GET` requests so loading states remain briefly visible during local/runtime demos without slowing workflow mutations.

### Work Queue badge

- Added role-aware `My Work` attention badge backed by existing `GET /api/work-queue?pageSize=1`.
- Removed temporary click-dismiss behavior after requirement change.
- Badge now remains visible until queue count naturally falls to zero after work is attended.

### Dashboard density

- Extended non-production demo refresher with stable dashboard-only billing requests, invoices, and audit logs distributed across recent 1, 3, 6, and 12 month windows.
- Kept seeded workload counts stable by leaving dashboard-only records outside Accounts/Manager pending queues.
- No EF migration needed because static `HasData` seed records were not changed.

### Reader-facing docs

- Updated README capability notes for smoother UI transitions, persistent Work Queue attention badge, and denser demo dashboard activity.
- Refreshed README behaviour-flow image reference to current generic product flow.
- Added `session-flow.svg` and generated `session-flow.png` with generic title `FlowLedger Behaviour Flow`.

## Verification

### Frontend

- `cd frontend/flowledger-web && npm test -- --run`
  - Passed: 14 files, 36 tests.
- `cd frontend/flowledger-web && npm run lint`
  - Passed.
- `cd frontend/flowledger-web && npm run build`
  - Passed.
  - Existing Vite chunk-size warning remains; no build failure.

### Backend

- `docker run --rm -e TESTCONTAINERS_RYUK_DISABLED=true -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal -v "$PWD:/src" -v /var/run/docker.sock:/var/run/docker.sock -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet test FlowLedger.sln --logger "console;verbosity=minimal"'`
  - Passed: 96 tests.

### Runtime smoke

- `docker compose up --build -d`
  - Passed.
- Verified:
  - `GET http://localhost:8080/health` returned `200`.
  - `GET http://localhost:5173/` and `GET http://localhost:5173/login` returned `200`.
  - Dashboard summaries varied by period:
    - 1 month: `19` requests, `982000` invoice amount.
    - 3 months: `21` requests, `1112000` invoice amount.
    - 6 months: `23` requests, `1360000` invoice amount.
    - 12 months: `25` requests, `1525000` invoice amount.
  - Current workload stayed invariant across periods: Accounts `3`, Manager `2`.
  - Accounts work queue endpoint with `pageSize=1` returned `totalCount = 3`.
- Browser-driven UI smoke was intentionally skipped per latest user instruction.
- `docker compose down -v --remove-orphans`
  - Passed. Local containers, network, and SQL Server volume were removed after smoke verification.

## Follow-up adjustments

### Settings page

- Removed the standalone loading block above the settings grid.
- Billing settings loading skeleton now renders inside the `Billing Settings` card where the form will appear.
- Rule-impact card now shows its own inline loading skeleton instead of temporary placeholder values.

### Request and invoice filters

- Added request list filters for created date range and min/max total amount.
- Added invoice list filters for issued date range and min/max total amount.
- Extended backend request and invoice query contracts so list endpoints and CSV export endpoints honor the same date and amount filters.
- Refined filter-toolbar layout so date and amount ranges render as compact paired rows, checkbox options stack in a final narrow column, and medium/tablet widths avoid oversized or awkward wrapping.

### Follow-up verification

- Frontend:
  - `cd frontend/flowledger-web && npm test -- --run`
    - Passed: 17 files, 41 tests.
  - `cd frontend/flowledger-web && npm run lint`
    - Passed.
  - `cd frontend/flowledger-web && npm run build`
    - Passed.
- Backend:
  - `docker run --rm -e TESTCONTAINERS_RYUK_DISABLED=true -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal -v "$PWD:/src" -v /var/run/docker.sock:/var/run/docker.sock -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet test FlowLedger.sln --logger "console;verbosity=minimal"'`
    - Passed: 98 tests.
- Runtime smoke:
  - `docker compose up --build -d`
    - Passed.
  - Verified:
    - `GET /health` returned `200`.
    - frontend `/app/settings` and `/app/requests` returned `200`.
    - filtered billing requests endpoint returned only in-range rows.
    - filtered invoices endpoint returned only in-range rows.
    - billing-request and invoice CSV exports still returned headers and completed successfully with filters applied.

### Responsive layout verification

- `cd frontend/flowledger-web && npm run lint`
  - Passed after responsive filter-grid adjustments.
- `cd frontend/flowledger-web && npm run build`
  - Passed after responsive filter-grid adjustments.

### Corrective filter layout pass

- Reworked request and invoice filter toolbars into two clear rows:
  - primary filters: search, status, client.
  - advanced filters: date range, amount range, list options.
- Date and amount range controls now live below the primary filters and spread across available width.
- Request checkboxes remain in one final options column with row-size control below them.
- Tablet layout now gives search a full row, places status/client side by side, then places range controls below without squeezing native date inputs.

### Corrective layout verification

- `cd frontend/flowledger-web && npm test -- --run`
  - Passed: 17 files, 41 tests.
- `cd frontend/flowledger-web && npm run lint`
  - Passed.
- `cd frontend/flowledger-web && npm run build`
  - Passed.
  - Existing Vite chunk-size warning remains; no build failure.

### Second corrective filter layout pass

- Replaced the dense request and invoice table toolbars with page-specific filter panels.
- Restored the shared `DataTableToolbar` to its simpler default grid so other tables are not affected by request/invoice-specific layout needs.
- Request and invoice pages now use:
  - first row: search, status, client.
  - second row: date range, amount range, final list-options/action area.
- Date and amount range controls remain full-width on smaller screens, move to two wide columns on large screens, and only use the final three-column layout on extra-wide screens.
- Request checkboxes stay stacked in a single final options area, with rows and CSV export below them.
- CSV export buttons now accept optional width classes so dense filter panels can align actions without affecting other pages.

### Second corrective layout verification

- `cd frontend/flowledger-web && npm test -- --run`
  - Passed: 17 files, 41 tests.
- `cd frontend/flowledger-web && npm run lint`
  - Passed.
- `cd frontend/flowledger-web && npm run build`
  - Passed.
  - Existing Vite chunk-size warning remains; no build failure.
