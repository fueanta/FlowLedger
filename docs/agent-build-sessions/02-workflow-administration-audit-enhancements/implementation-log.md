# Session 02 Implementation Log - Workflow, Administration & Audit Enhancements

Read from the bottom first. Newest phase status, verification notes, and blockers should be appended at the end of this file.

## Session setup

### Planned

- Created Session 02 plan for workflow, administration, audit, settings, preferences, standardized tables, CSV export, and invoice PDF/print enhancements.
- Active plan: `docs/agent-build-sessions/02-workflow-administration-audit-enhancements/workflow_admin_audit_enhancements_plan.md`.

### Verification

- Docs-only planning step.
- No app build or tests required yet.

### Notes

- Implementation must proceed one phase at a time.
- After each phase, append built items, verification commands, outcomes, blockers, and whether the phase is signed off.
- Do not commit or push until explicitly requested.

## Plan expansion after review feedback

### Built

- Expanded the Session 02 plan into a detailed build-spec style plan.
- Added Phase 0 for recent demo seed data and dashboard metric scope clarity.
- Added explicit entity, field, API, UI component, table, export, test, and sign-off planning.

### Verification

- Docs-only planning update.
- No app build or tests required.

### Notes

- Phase 0 must fix the current demo-data issue where default 1-month dashboard activity looks empty while 6-month activity shows old seeded data.
- Dashboard UI must clearly distinguish period-filtered activity from current-state pending workload.

## Phase 0: Demo seed recency and dashboard scope labels

### Built

- Added `IDateTimeProvider` and `SystemDateTimeProvider`.
- Added a Development/Docker demo seed date refresher that runs after migrations and password bootstrap.
- Refreshed seeded billing request, invoice, audit log, comment, and notification dates relative to current UTC date.
- Added missing demo workflow audit logs after migration refresh so seeded final-state workflow records have matching audit history.
- Added dashboard period metadata and metric scope metadata to the dashboard summary response.
- Updated dashboard UI to split:
  - `Period Activity`
  - `Current Workload`
- Added metric scope badges:
  - `Period filtered`
  - `Current state`
- Added frontend dashboard coverage for section labels and scope chips.
- Added backend dashboard coverage for default 1-month seeded activity, 1-month vs 6-month current workload invariance, and refreshed demo audit coverage.

### Verification

- Passed: frontend tests.
  - Command: `cd frontend/flowledger-web && npm test -- --run`
  - Result: 12 tests passed, 0 failed.
- Passed: frontend lint.
  - Command: `cd frontend/flowledger-web && npm run lint`
  - Result: ESLint passed.
- Passed: frontend production build.
  - Command: `cd frontend/flowledger-web && npm run build`
  - Result: TypeScript and Vite production build succeeded.
  - Note: existing Vite chunk-size warning remains.
- Passed: backend build through Docker SDK container.
  - Command: `docker run --rm -v "$PWD:/src" -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet build FlowLedger.sln'`
  - Result: build succeeded with 0 warnings and 0 errors.
- Passed: full backend test suite through Docker SDK container.
  - Command: `docker run --rm -e TESTCONTAINERS_RYUK_DISABLED=true -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal -v "$PWD:/src" -v /var/run/docker.sock:/var/run/docker.sock -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet test FlowLedger.sln --logger "console;verbosity=minimal"'`
  - Result: 40 tests passed, 0 failed.
- Passed: Docker Compose runtime smoke.
  - Command: `docker compose up --build -d`
  - Result: SQL Server became healthy, API started, frontend started.
  - Frontend route smoke: `curl -fsSI http://localhost:5173/app/dashboard` returned `HTTP/1.1 200 OK`.
  - API dashboard smoke: default period returned non-zero activity and stable current pending counts across 1-month and 6-month periods.
  - Smoke output: `phase0 dashboard smoke ok 1 17 839000.0 470000.0 3 2`.
  - Cleanup: `docker compose down -v --remove-orphans`.

### Notes

- Phase 0 is signed off.
- Conventional Commit message when commit is requested: `feat: refresh demo dashboard seed data`

## Phase 1: Client Management

### Built

- Added `ClientStatus` with `Active`, `Inactive`, and `Archived`.
- Evolved the existing `Customer` persistence model into the Client-facing model without breaking existing billing request and invoice foreign keys.
- Added client fields:
  - `ContactPerson`
  - `TaxIdentifier`
  - `Status`
  - `UpdatedAtUtc`
  - `ArchivedAtUtc`
  - `ArchivedByUserId`
- Updated seeded customers with client contact people, tax identifiers, active status, and updated timestamps.
- Added EF migration `AddClientManagementFields`.
- Added Client DTOs, validators, paged query, create/update/archive service methods, and role rules:
  - Sales can create.
  - Accounts can create/edit.
  - Manager can view only.
  - Admin can create/edit/archive.
- Added `/api/clients` endpoints:
  - `GET /api/clients`
  - `GET /api/clients/{id}`
  - `POST /api/clients`
  - `PUT /api/clients/{id}`
  - `POST /api/clients/{id}/archive`
- Kept legacy `GET /api/customers` compatibility for current request/invoice selectors while frontend now uses active clients.
- Modified billing request create/update to require active clients.
- Renamed frontend navigation and route from Customers to Clients.
- Added Clients page with search, status filter, sort controls, table layout, create/edit client dialog, and archive confirmation.
- Updated request and invoice screens to use Client terminology and active-client selectors.
- Added frontend client form schema and validation tests.

### Verification

- Passed: backend build through Docker SDK container.
  - Command: `docker run --rm -v "$PWD:/src" -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet build FlowLedger.sln'`
  - Result: build succeeded with 0 warnings and 0 errors.
- Passed: full backend test suite through Docker SDK container.
  - Command: `docker run --rm -e TESTCONTAINERS_RYUK_DISABLED=true -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal -v "$PWD:/src" -v /var/run/docker.sock:/var/run/docker.sock -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet test FlowLedger.sln --logger "console;verbosity=minimal"'`
  - Result: 48 tests passed, 0 failed.
- Passed: frontend tests.
  - Command: `cd frontend/flowledger-web && npm test -- --run`
  - Result: 14 tests passed, 0 failed.
- Passed: frontend lint.
  - Command: `cd frontend/flowledger-web && npm run lint`
  - Result: ESLint passed.
- Passed: frontend production build.
  - Command: `cd frontend/flowledger-web && npm run build`
  - Result: TypeScript and Vite production build succeeded.
  - Note: existing Vite chunk-size warning remains.
- Passed: Docker Compose runtime smoke.
  - Command: `docker compose up --build -d`
  - Result: SQL Server became healthy, API started, frontend started.
  - API smoke:
    - Manager could list clients.
    - Sales could create a client.
    - Manager edit returned `403`.
    - Accounts edit returned `204`.
    - Admin archive returned `204`.
    - Creating a billing request with the archived client returned `400`.
  - Frontend route smoke: `HEAD http://localhost:5173/app/clients` returned `200`.
  - Smoke output: `phase1 smoke ok clients=6 managerEdit=403 accountsEdit=204 archive=204 billingGuard=400 web=200`.
  - Cleanup: `docker compose down -v --remove-orphans`.

### Notes

- Phase 1 is signed off.
- In-app browser control tool was not exposed by tool discovery in this environment, so UI runtime verification used the production frontend build and HTTP route smoke.
- Conventional Commit message when commit is requested: `feat: add client management`

## Phase 2: Configurable Settings

### Built

- Added typed system settings DTOs, validator, and service over the existing `AppSettings` table.
- Added seeded settings:
  - `Billing.VatPercentage = 15`
  - `Billing.ManagerApprovalThreshold = 100000`
  - `Billing.InvoiceDueDays = 30`
- Added `GET /api/settings` for all internal users.
- Added `PUT /api/settings` for Admin users only.
- Replaced hardcoded VAT in billing request total calculation with configured VAT percentage.
- Replaced hardcoded manager approval threshold with configured threshold at approval time.
- Replaced hardcoded invoice due days with configured due days at invoice generation.
- Added invoice snapshot fields:
  - `VatPercentage`
  - `DueDays`
- Added EF migration `AddConfigurableBillingSettings`.
- Updated invoice detail API and UI to display VAT rate and due period snapshots.
- Rebuilt Settings UI as a live settings surface:
  - Admin editable fields.
  - Read-only display for non-Admin roles.
  - Current rule impact text.
- Added backend tests for:
  - configured VAT total calculation.
  - configured threshold routing.
  - invoice VAT/due-day snapshots.
  - settings read/update authorization.
  - existing invoice values remaining unchanged after settings update.
- Added frontend settings validation tests.

### Verification

- Passed: backend build through Docker SDK container.
  - Command: `docker run --rm -v "$PWD:/src" -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet build FlowLedger.sln'`
  - Result: build succeeded with 0 warnings and 0 errors.
- Passed: full backend test suite through Docker SDK container.
  - Command: `docker run --rm -e TESTCONTAINERS_RYUK_DISABLED=true -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal -v "$PWD:/src" -v /var/run/docker.sock:/var/run/docker.sock -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet test FlowLedger.sln --logger "console;verbosity=minimal"'`
  - Result: 55 tests passed, 0 failed.
- Passed: frontend tests.
  - Command: `cd frontend/flowledger-web && npm test -- --run`
  - Result: 16 tests passed, 0 failed.
- Passed: frontend lint.
  - Command: `cd frontend/flowledger-web && npm run lint`
  - Result: ESLint passed.
- Passed: frontend production build.
  - Command: `cd frontend/flowledger-web && npm run build`
  - Result: TypeScript and Vite production build succeeded.
  - Note: existing Vite chunk-size warning remains.
- Passed: Docker Compose runtime smoke.
  - Command: `docker compose up --build -d`
  - Result: SQL Server became healthy, API started, frontend started.
  - API smoke:
    - Admin updated settings to VAT `20`, threshold `250000`, due days `10`.
    - Accounts could read settings.
    - Sales created and submitted a request with subtotal `120000`.
    - Accounts approval generated invoice directly because configured threshold was `250000`.
    - Invoice detail returned total `144000`, VAT `20`, and due days `10`.
  - Frontend route smoke: `HEAD http://localhost:5173/app/settings` returned `200`.
  - Smoke output: `phase2 smoke ok settings=20/250000/10 request=InvoiceGenerated invoiceTotal=144000 vat=20 dueDays=10 web=200`.
  - Cleanup: `docker compose down -v --remove-orphans`.

### Notes

- Phase 2 is signed off.
- Conventional Commit message when commit is requested: `feat: add configurable billing settings`

## Phase 3: Enrollment and User Administration

### Built

- Added `EnrollmentRequest` entity and `EnrollmentRequestStatus` enum.
- Added public enrollment registration API:
  - `POST /api/enrollment-requests`
- Added Admin enrollment review APIs:
  - `GET /api/enrollment-requests`
  - `GET /api/enrollment-requests/{id}`
  - `POST /api/enrollment-requests/{id}/approve`
  - `POST /api/enrollment-requests/{id}/reject`
- Added user administration status model with `UserStatus`, `UpdatedAtUtc`, `DeactivatedAtUtc`, `DeactivatedByUserId`, `LastLoginAtUtc`, and `EnrollmentRequestId`.
- Added Admin user management APIs:
  - `GET /api/users`
  - `GET /api/users/{id}`
  - `PUT /api/users/{id}/role`
  - `POST /api/users/{id}/activate`
  - `POST /api/users/{id}/deactivate`
- Enforced admin rules:
  - Pending enrollment users cannot log in.
  - Rejected enrollment users cannot log in.
  - Approved enrollment creates or reactivates an active user.
  - Inactive users cannot log in.
  - Admin cannot deactivate their own account.
  - Admin cannot remove the last active Admin account.
- Added generic audit targets to `AuditLog`:
  - `EntityType`
  - `EntityId`
  - `EntityNumber`
  - `ActorDisplayName`
  - `BeforeStatus`
  - `AfterStatus`
- Added audit action types for enrollment approval/rejection and user activation/deactivation/role changes.
- Added EF migrations:
  - `AddEnrollmentAndUserAdministration`
  - `AddGenericAuditTarget`
- Added public Register page linked from Login.
- Added Admin Enrollment Requests page with search, status/role filters, sorting, approve, and reject actions.
- Added Admin Users page with search, role/status filters, sorting, role update, activate, and deactivate actions.
- Added Admin-only navigation for Enrollment and Users.
- Added frontend registration schema and tests.

### Verification

- Passed: backend build through Docker SDK container.
  - Command: `docker run --rm -v "$PWD:/src" -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet build FlowLedger.sln'`
  - Result: build succeeded with 0 warnings and 0 errors.
- Passed: full backend test suite through Docker SDK container with Docker socket and host override for Testcontainers.
  - Command: `docker run --rm -e TESTCONTAINERS_RYUK_DISABLED=true -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal -v "$PWD:/src" -v /Users/mutasim/.docker/run/docker.sock:/var/run/docker.sock -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet test FlowLedger.sln'`
  - Result: 61 tests passed, 0 failed.
- Passed: frontend tests.
  - Command: `cd frontend/flowledger-web && npm test -- --run`
  - Result: 18 tests passed, 0 failed.
- Passed: frontend lint.
  - Command: `cd frontend/flowledger-web && npm run lint`
  - Result: ESLint passed.
- Passed: frontend production build.
  - Command: `cd frontend/flowledger-web && npm run build`
  - Result: TypeScript and Vite production build succeeded.
  - Note: existing Vite chunk-size warning remains.
- Passed: Docker Compose runtime smoke.
  - Command: `docker compose up --build -d`
  - Result: SQL Server became healthy, API started, frontend started.
  - API smoke:
    - Public registration returned `201`.
    - Pending enrollment login returned `401`.
    - Admin approval returned `204`.
    - Approved user login succeeded with assigned role `Accounts`.
    - Admin deactivation returned `204`.
    - Deactivated user login returned `401`.
    - Admin self-deactivation returned `400`.
  - Frontend route smoke:
    - `/register` returned `200`.
    - `/app/enrollment-requests` returned `200`.
    - `/app/users` returned `200`.
  - Smoke output: `phase3 smoke ok register=201 pendingLogin=401 approve=204 approvedRole=Accounts deactivate=204 deactivatedLogin=401 selfDeactivate=400 web=200/200/200`.
  - Cleanup: `docker compose down -v --remove-orphans`.

### Notes

- Phase 3 is signed off.
- Conventional Commit message when commit is requested: `feat: add enrollment and user administration`

## Phase 4: Assignment, My Work Queue, and Audit Enforcement

### Built

- Added `WorkflowQueue` enum with `None`, `Sales`, `Accounts`, and `Manager`.
- Added billing request assignment/workflow metadata:
  - `AssignedQueue`
  - `AssignedAtUtc`
  - `SubmittedByUserId`
  - `AccountsReviewedByUserId`
  - `ManagerReviewedByUserId`
  - `LastWorkflowActionAtUtc`
- Added EF migration `AddWorkflowAssignmentMetadata`.
- Updated workflow routing:
  - Draft/rejected requests assign to Sales queue.
  - Submit assigns to Accounts queue.
  - Accounts approval under threshold generates invoice and clears queue.
  - Accounts approval over threshold assigns to Manager queue.
  - Manager approval generates invoice and clears queue.
  - Reject assigns back to Sales queue.
  - Mark paid keeps queue cleared.
- Added `IWorkflowAuditWriter` and production `WorkflowAuditWriter`.
- Updated billing request and invoice workflow writes to use audit writer inside transaction-backed save.
- Added audit rollback coverage by replacing `IWorkflowAuditWriter` with a throwing test writer.
- Updated seed data and demo seed refresher so seeded records include queue metadata and assignment audit rows.
- Added `GET /api/work-queue` with role-aware queue filtering:
  - Sales sees own Sales queue.
  - Accounts sees Accounts queue.
  - Manager sees Manager queue.
  - Admin sees all active queues or selected queue.
- Extended billing request list/detail DTOs with assignment queue, assigned time, and last workflow action time.
- Added My Work Queue UI page with search, Admin queue filter, role-aware approve/reject actions, and route `/app/work-queue`.
- Added queue metadata to request list and request detail UI.

### Verification

- Passed: backend build through Docker SDK container.
  - Command: `docker run --rm -v "$PWD:/src" -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet build FlowLedger.sln'`
  - Result: build succeeded with 0 warnings and 0 errors.
- Passed: full backend test suite through Docker SDK container with Docker socket and host override for Testcontainers.
  - Command: `docker run --rm -e TESTCONTAINERS_RYUK_DISABLED=true -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal -v "$PWD:/src" -v /Users/mutasim/.docker/run/docker.sock:/var/run/docker.sock -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet test FlowLedger.sln'`
  - Result: 63 tests passed, 0 failed.
- Passed: frontend tests.
  - Command: `cd frontend/flowledger-web && npm test -- --run`
  - Result: 18 tests passed, 0 failed.
- Passed: frontend lint.
  - Command: `cd frontend/flowledger-web && npm run lint`
  - Result: ESLint passed.
- Passed: frontend production build.
  - Command: `cd frontend/flowledger-web && npm run build`
  - Result: TypeScript and Vite production build succeeded.
  - Note: existing Vite chunk-size warning remains.
- Passed: Docker Compose runtime smoke.
  - Command: `docker compose up --build -d`
  - Result: SQL Server became healthy, API started, frontend started.
  - API smoke:
    - Low-value Sales request submitted to Accounts queue.
    - Accounts queue API returned the low-value request.
    - Accounts approval generated invoice and cleared queue.
    - High-value Sales request submitted, then Accounts approval moved it to Manager queue.
    - Manager queue API returned the high-value request.
    - Manager approval generated invoice and cleared queue.
  - Frontend route smoke: `/app/work-queue` returned `200`.
  - Smoke output: `phase4 smoke ok low=InvoiceGenerated/None highBefore=ManagerApproval/Manager highAfter=InvoiceGenerated/None web=200`.
  - Cleanup: `docker compose down -v --remove-orphans`.

### Notes

- Phase 4 is signed off.
- Conventional Commit message when commit is requested: `feat: add workflow assignment queues`

## Phase 5: Preferences and Standard Data Table Foundation

### Built

- Added `UserPreference` entity with:
  - `UserId`
  - `DefaultDashboardPeriodMonths`
  - `DefaultLandingPage`
  - `RowsPerPage`
  - `CreatedAtUtc`
  - `UpdatedAtUtc`
- Added EF migration `AddUserPreferences`.
- Added preference API:
  - `GET /api/preferences/me`
  - `PUT /api/preferences/me`
- Added role defaults:
  - Sales: `/app/requests`, 1-month dashboard period, 25 rows.
  - Accounts: `/app/dashboard`, 1-month dashboard period, 25 rows.
  - Manager: `/app/dashboard`, 1-month dashboard period, 25 rows.
  - Admin: `/app/dashboard`, 1-month dashboard period, 50 rows.
- Added validation:
  - Dashboard period must be `1`, `3`, `6`, or `12`.
  - Rows per page must be `10`, `25`, `50`, or `100`.
  - Non-Admin users cannot save Admin-only landing pages.
- Added shared backend paging contracts:
  - `PagedQuery`
  - `SortDirection`
- Installed and locked `@tanstack/react-table`.
- Added reusable data table foundation:
  - `DataTable`
  - `DataTableToolbar`
  - `DataTablePagination`
  - `DataTablePageSizeSelect`
  - `DataTableSearch`
  - `DataTableSortableHeader`
  - `DataTableExportButton`
  - `dataTableState`
  - `dataTableTypes`
- Added frontend preferences API helpers.
- Updated Docker-compatible `package-lock.json` using the same Node Alpine platform used by the frontend Dockerfile.

### Verification

- Passed: backend build through Docker SDK container.
  - Command: `docker run --rm -v "$PWD:/src" -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet build FlowLedger.sln'`
  - Result: build succeeded with 0 warnings and 0 errors.
- Passed: full backend test suite through Docker SDK container with Docker socket and host override for Testcontainers.
  - Command: `docker run --rm -e TESTCONTAINERS_RYUK_DISABLED=true -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal -v "$PWD:/src" -v /Users/mutasim/.docker/run/docker.sock:/var/run/docker.sock -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet test FlowLedger.sln'`
  - Result: 67 tests passed, 0 failed.
- Passed: frontend tests.
  - Command: `cd frontend/flowledger-web && npm test -- --run`
  - Result: 23 tests passed, 0 failed.
- Passed: frontend lint.
  - Command: `cd frontend/flowledger-web && npm run lint`
  - Result: ESLint passed.
- Passed: frontend production build.
  - Command: `cd frontend/flowledger-web && npm run build`
  - Result: TypeScript and Vite production build succeeded.
  - Note: existing Vite chunk-size warning remains.
- Passed: Docker Compose runtime smoke.
  - Command: `docker compose up --build -d`
  - Result: SQL Server became healthy, API started, frontend started, and frontend Docker `npm ci` succeeded with updated lockfile.
  - API smoke:
    - Sales preference default returned `/app/requests` and `25` rows.
    - Admin preference default returned `50` rows.
    - Sales preference update persisted `/app/work-queue`, 6-month dashboard period, and `100` rows.
    - Sales attempt to save Admin-only `/app/users` landing page returned `400`.
  - Frontend route smoke: `/app/work-queue` returned `200`.
  - Smoke output: `phase5 smoke ok salesDefault=/app/requests/25 updateRows=100 invalid=400 routeWidths=375:200,768:200,1024:200,1440:200`.
- Passed: viewport smoke through temporary Playwright Docker container.
  - Command: `docker run -i --rm mcr.microsoft.com/playwright:v1.57.0-noble sh -lc 'cd /tmp && npm init -y >/dev/null && npm install playwright@1.57.0 >/dev/null && node -'`
  - Result:
    - `viewport 375 ok overflow=false`
    - `viewport 768 ok overflow=false`
    - `viewport 1024 ok overflow=false`
    - `viewport 1440 ok overflow=false`
  - Cleanup: `docker compose down -v --remove-orphans`.

### Notes

- Phase 5 is signed off.
- Conventional Commit message when commit is requested: `feat: add preferences and data table foundation`
