# Implementation Log

## Phase 1: Skeleton and Docker

### Built

- Created the planned `backend/` solution structure with API, Application, Domain, Infrastructure, and Tests projects.
- Pinned the repository to .NET SDK `8.0.422` with `global.json`.
- Added a minimal ASP.NET Core `/health` endpoint returning `{ "status": "ok" }`.
- Created the React Vite TypeScript app with a simple FlowLedger landing page.
- Added API and frontend Dockerfiles, nginx SPA fallback, root `docker-compose.yml`, `.env.example`, and `.gitignore`.
- Added a backend integration test for `/health`.

### Verification

- Passed: `cd backend && dotnet build && dotnet test --no-build`.
  - Result: build succeeded with 0 warnings and 0 errors.
  - Result: 1 test passed, 0 failed.
- Passed: `cd frontend/flowledger-web && npm run build`.
  - Result: TypeScript and Vite production build succeeded.
- Passed: local API runtime smoke check.
  - Command: `dotnet run --project FlowLedger.Api/FlowLedger.Api.csproj --no-build`.
  - Checked: `curl http://localhost:5296/health`.
  - Result: returned `{ "status": "ok" }`.
- Passed: local frontend runtime smoke check.
  - Command: `npm run dev -- --host 127.0.0.1`.
  - Checked: `curl http://127.0.0.1:5173/`.
  - Checked: in-app browser loaded the page, found FlowLedger content, and reported 0 console errors.
- Passed: Docker Compose runtime smoke check after Docker Desktop was installed.
  - Command: `docker compose up --build -d`.
  - Result: SQL Server container reached healthy state.
  - Result: API and web images built successfully.
  - Result: `docker compose ps` showed `flowledger-sqlserver`, `flowledger-api`, and `flowledger-web` running.
  - Checked: `curl http://localhost:8080/health`.
  - Result: returned `{ "status": "ok" }`.
  - Checked: `curl -I http://localhost:5173`.
  - Result: returned `HTTP/1.1 200 OK`.
  - Checked: in-app browser loaded Docker-served frontend, found FlowLedger content, and reported 0 console errors.
  - Cleanup: `docker compose down` stopped and removed the containers/network.
  - Note: Docker warned that SQL Server image platform is `linux/amd64` on host `linux/arm64/v8`, but the container still started and became healthy under Docker Desktop.
- Passed: Docker Compose security hardening verification.
  - Removed committed SQL Server password and JWT signing key from `docker-compose.yml`.
  - Updated `.env.example` to contain placeholders only.
  - Added `docs/deployment-security.md` for local secret handling and production deployment expectations.
  - Checked: repository scan no longer finds the previous literal development SQL password or JWT key.
  - Command: `SQLSERVER_SA_PASSWORD='...' JWT_KEY='...' docker compose up --build -d`.
  - Result: SQL Server reached healthy state, API and web started, `/health` returned `{ "status": "ok" }`, and frontend returned `HTTP/1.1 200 OK`.
  - Cleanup: `docker compose down` stopped and removed the containers/network.

### Handoff

- Phase 1 is signed off and ready for Phase 2.
- Git history was squashed to one commit: `ceb23d5 feat: scaffold FlowLedger phase 1 foundation`.
- GitHub repository: `https://github.com/fueanta/FlowLedger`.
- Remote: `origin` points to `https://github.com/fueanta/FlowLedger.git`.
- Branch: `main`.
- Note: `.codex/skills/ui-ux-pro-max` is tracked so project UI/UX guidance survives clone. `.gitattributes` marks it as vendored for GitHub Linguist language stats.
- Next phase from `docs/agent-build-sessions/01-initial-flowledger-build/erp_workflow_build_plan.md`: Phase 2, Backend domain and database.

## Phase 2: Backend domain and database

### Built

- Added Domain enums: `RoleName`, `BillingRequestStatus`, `AuditActionType`, and `InvoiceStatus`.
- Added Domain entities: `User`, `Customer`, `BillingRequest`, `BillingRequestLineItem`, `Comment`, `AuditLog`, `Invoice`, and `Notification`.
- Added `ApprovalRules` with `ManagerApprovalThreshold = 100000m` and `VatRate = 0.15m`.
- Added EF Core SQL Server persistence with `FlowLedgerDbContext`.
- Added EF configurations for table names, string lengths, decimal precision, indexes, unique request/invoice numbers, and delete behavior.
- Added deterministic seed data:
  - 4 users matching the plan emails and roles.
  - 6 customers matching the plan.
  - 17 billing requests with the exact planned status distribution.
  - 17 line items, 7 invoices, 3 comments, 6 audit logs, and 3 notifications.
  - Preserved planned journey examples: `BR-2026-0004`, `BR-2026-0006`, `BR-2026-0008`, and `INV-2026-0003`.
- Added EF migration `InitialCreate`.
- Wired the API to run EF migrations on startup when `ConnectionStrings__DefaultConnection` is configured.
- Added SQL Server-backed integration tests using Testcontainers.

### Verification

- Passed: `cd backend && dotnet build`.
  - Result: build succeeded with 0 warnings and 0 errors.
- Passed: `cd backend && dotnet test --no-build`.
  - Result: 4 tests passed, 0 failed.
  - Coverage added:
    - `/health` still returns `{ "status": "ok" }`.
    - EF migration creates the expected 8 domain tables in SQL Server.
    - EF migration seeds expected baseline counts.
    - EF migration seeds the planned workflow example records.
- Passed: `cd frontend/flowledger-web && npm run build`.
  - Result: TypeScript and Vite production build succeeded.
- Passed: Docker Compose runtime and database verification from a clean SQL Server volume.
  - Command: generated temporary local secrets in shell, then ran `docker compose down -v --remove-orphans` and `docker compose up --build -d`.
  - Result: SQL Server reached healthy state, API and web containers started, and API migration created `FlowLedgerDb`.
  - Checked: `curl http://localhost:8080/health`.
  - Result: returned `{ "status": "ok" }`.
  - Checked: `curl -I http://localhost:5173`.
  - Result: returned `HTTP/1.1 200 OK`.
  - Checked SQL Server with `sqlcmd`.
  - Result: 8 expected tables found.
  - Result: seed counts matched: 4 users, 6 customers, 17 billing requests, 7 invoices.
  - Result: planned journey rows found:
    - `BR-2026-0004` total `45000`, status `AccountsReview`.
    - `BR-2026-0006` total `180000`, status `AccountsReview`.
    - `BR-2026-0008` total `65000`, status `Rejected`.
    - `INV-2026-0003` status `Issued`.
  - Cleanup: `docker compose down -v --remove-orphans` stopped and removed containers, network, and SQL Server volume.

### Notes

- `dotnet-ef` was not installed globally. Per repo instruction, no global tool was installed. A temporary tool-path install under `/tmp/flowledger-dotnet-tools` was used only to generate the migration.
- Phase 2 is signed off and ready for Phase 3 after commit.

### Re-verification on 2026-06-12

- Passed: `dotnet build backend/FlowLedger.sln`.
  - Result: build succeeded with 0 warnings and 0 errors.
- Passed: `cd frontend/flowledger-web && npm run build`.
  - Result: TypeScript and Vite production build succeeded.
- Initial `dotnet test backend/FlowLedger.sln --no-build` attempt failed because Docker Desktop was not running.
  - Error: Testcontainers could not connect to Docker daemon at `/Users/mutasim/.docker/run/docker.sock`.
  - Action: started Docker Desktop. No install performed.
- Passed after Docker started: `dotnet test backend/FlowLedger.sln --no-build`.
  - Result: 4 tests passed, 0 failed.
- Passed: Docker Compose clean-volume verification.
  - Command: generated temporary local secrets in shell, then ran `docker compose down -v --remove-orphans` and `docker compose up --build -d`.
  - Result: API returned `{ "status": "ok" }`.
  - Result: frontend returned `HTTP/1.1 200 OK`.
  - Result: SQL Server had 8 expected tables.
  - Result: seed counts matched: 4 users, 6 customers, 17 billing requests, 7 invoices.
  - Result: planned journey rows still matched: `BR-2026-0004`, `BR-2026-0006`, `BR-2026-0008`, and `INV-2026-0003`.
  - Cleanup: `docker compose down -v --remove-orphans` stopped and removed containers, network, and SQL Server volume.
- Phase 2 remains signed off and ready for Phase 3.

## Phase 3: Auth

### Built

- Added Application auth contracts and DTOs:
  - `IAuthService`
  - `IJwtTokenGenerator`
  - `LoginRequestDto`
  - `LoginResponseDto`
  - `UserDto`
- Added seeded-user auth service.
  - Accepts only active users with database-backed password hashes and salts.
  - Does not use hardcoded app passwords.
  - Bootstraps local/demo seeded-user password hashes from environment values when provided.
- Added JWT token generation with required claims:
  - `sub`
  - `email`
  - `name`
  - `role`
- Added API auth endpoints:
  - `POST /api/auth/login`
  - `GET /api/auth/me`
- Added JWT bearer authentication and role policies:
  - `SalesOnly`
  - `AccountsOnly`
  - `ManagerOnly`
  - `InternalUser`
- Added Swagger/OpenAPI support with bearer token security scheme.
- Enabled Swagger UI outside Production only.
- Configured enum JSON serialization so API responses return role names like `Sales`, not numeric enum values.
- Kept secrets out of source:
  - Production requires `Jwt__Key`.
  - Docker Compose still requires `JWT_KEY`.
  - Non-production without `Jwt__Key` uses an ephemeral in-memory key so `/health` and local smoke runs can still start without a committed secret.

### Verification

- Passed: `dotnet restore backend/FlowLedger.sln`.
  - Result: restore succeeded after adding JWT Bearer and Swagger packages.
- Passed: `dotnet build backend/FlowLedger.sln --no-restore`.
  - Result: build succeeded with 0 warnings and 0 errors.
- Initial backend test run failed before fixes:
  - Command: `dotnet test backend/FlowLedger.sln --no-build`.
  - Result: 8 passed, 4 failed.
  - Cause: auth integration test configuration was applied too late for minimal hosting, and Swagger assertion expected compact JSON spacing.
  - Fix: moved test configuration to environment variables before `WebApplicationFactory` startup and adjusted Swagger assertion.
- Passed after fixes and final hardening: `dotnet test backend/FlowLedger.sln --no-build`.
  - Result: 12 tests passed, 0 failed.
  - Coverage added:
    - Auth service accepts a test-only user whose password hash is stored in the test database.
    - Auth service rejects wrong password.
    - Auth service resolves current test user.
    - `POST /api/auth/login` returns JWT and test user.
    - JWT contains `sub`, `email`, `name`, and `role` claims.
    - JWT expiry uses database setting `Jwt.AccessTokenMinutes`.
    - Invalid login returns `401 Unauthorized`.
    - `GET /api/auth/me` returns current user with a valid token.
    - `GET /api/auth/me` returns `401 Unauthorized` without a token.
    - Swagger document includes bearer security scheme.
- Passed: `cd frontend/flowledger-web && npm run build`.
  - Result: TypeScript and Vite production build succeeded.
- Passed: Docker Compose runtime smoke check from a clean SQL Server volume.
  - Command: generated temporary local secrets in shell, then ran `docker compose down -v --remove-orphans` and `docker compose up --build -d`.
  - Result: SQL Server reached healthy state, API and web containers started, and API migration created/seeded the database.
  - Checked: `curl http://localhost:8080/health`.
  - Result: returned `{ "status": "ok" }`.
  - Checked: `curl http://localhost:8080/swagger/v1/swagger.json`.
  - Result: Swagger JSON contained the bearer security scheme.
  - Checked: `curl POST http://localhost:8080/api/auth/login` with `sales@flowledger.local` and a password supplied through `SeedUsers__SalesPassword`.
  - Result: returned an access token and user role `Sales`.
  - Checked: `curl http://localhost:8080/api/auth/me` with the returned bearer token.
  - Result: returned `sales@flowledger.local` with role `Sales`.
  - Checked: `curl -I http://localhost:5173`.
  - Result: returned `HTTP/1.1 200 OK`.
  - Checked: in-app browser loaded `http://localhost:8080/swagger`.
  - Result: Swagger UI showed `/api/auth/login`, `/api/auth/me`, and `Authorize` with 0 console errors.
  - Checked after final Docker image rebuild: Swagger UI `Try it out` for `POST /api/auth/login`.
  - Result: seeded credentials returned `200` and an `accessToken`.
  - Cleanup: `docker compose down -v --remove-orphans` stopped and removed containers, network, and SQL Server volume.

### Notes

- Phase 3 is implemented and verified.
- Per project instruction, changes are not committed yet. Commit only after explicit user signal.

## Phase 9: Tests

### Built

- Added backend integration tests for the remaining explicit Phase 9 API cases:
  - unauthenticated billing request list returns `401 Unauthorized`
  - Accounts cannot create billing requests
  - Manager cannot approve an `AccountsReview` request
- Expanded backend service-level workflow tests:
  - draft submit moves to `AccountsReview`
  - under-threshold Accounts approval generates an invoice
  - above-threshold Accounts approval moves to `ManagerApproval`
  - Manager approval generates an invoice
  - invalid resubmission throws a workflow conflict
  - rejected request can be resubmitted by Sales
- Added a frontend `StatusBadge` test because frontend tests already exist and the plan specifically names `StatusBadge` plus permission helpers when frontend tests are added.

### Verification

- Passed: backend restore/build through Docker SDK container.
  - Command: `docker run --rm -v "$PWD:/src" -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet restore FlowLedger.sln && dotnet build FlowLedger.sln --no-restore'`.
  - Result: build succeeded with 0 warnings and 0 errors.
- Passed: full backend test suite.
  - Command: `docker run --rm -e TESTCONTAINERS_RYUK_DISABLED=true -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal -v "$PWD:/src" -v /var/run/docker.sock:/var/run/docker.sock -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet test FlowLedger.sln --logger "console;verbosity=minimal"'`.
  - Result: 38 tests passed, 0 failed.
- Passed: frontend tests.
  - Command: `cd frontend/flowledger-web && npm test`.
  - Result: 11 tests passed, 0 failed.
- Passed: frontend lint.
  - Command: `cd frontend/flowledger-web && npm run lint`.
  - Result: ESLint passed with 0 errors.
- Passed: frontend production build.
  - Command: `cd frontend/flowledger-web && npm run build`.
  - Result: TypeScript and Vite production build succeeded.
  - Note: Vite still warns that one bundle chunk is over 500 kB because current assignment scope keeps pages and charts in one bundle.

### Notes

- Local `dotnet` is still unavailable in this shell. No global install was performed.
- Backend test coverage now directly maps to the Phase 9 unit and integration test list.
- Per project instruction, changes are not committed yet. Commit only after explicit user signal.

## Phase 10: Documentation polish

### Built

- Added root `README.md` with:
  - what was built
  - selected option
  - demo credential environment variable names
  - Docker run instructions
  - local development instructions
  - key user flows
  - architecture, backend, frontend, data model, and API overview
  - testing commands
  - known limitations
  - future improvements
  - AI tools and review process
- Added `docs/design-note.md` with the planned problem, option, roles, workflow, architecture, backend/frontend design, data model, security, testing, tradeoffs, limitations, future improvements, and AI usage sections.
- Added `docs/screenshots.md` with local screenshot targets and suggested review screenshots.
- Removed the old prompt-for-user section from `docs/agent-build-sessions/01-initial-flowledger-build/erp_workflow_build_plan.md`, keeping project requirements and final checklist.
- Updated stale `AGENTS.md` wording that previously said no local Git history existed.

### Verification

- Passed: Docker Compose build and runtime smoke.
  - Command: `docker compose up --build -d`.
  - Result: SQL Server became healthy, API started, and frontend started.
  - Frontend route smoke:
    - `http://localhost:5173/login` returned `200`.
    - `http://localhost:5173/app/dashboard` returned `200`.
  - API route smoke:
    - `http://localhost:8080/health` returned `200`.
    - `http://localhost:8080/swagger/index.html` returned `200`.
  - API workflow smoke:
    - Sales login succeeded.
    - Accounts login succeeded.
    - Manager login succeeded.
    - Admin login succeeded.
    - Sales created and submitted a low-value request.
    - Accounts approved it and invoice was generated.
    - Accounts marked the invoice paid.
    - Sales created and submitted a high-value request.
    - Accounts approved it and it moved to `ManagerApproval`.
    - Manager approved it and invoice was generated.
    - Sales created and submitted a request.
    - Accounts rejected it.
    - Sales revised it and resubmitted it to `AccountsReview`.
    - Invalid dashboard period returned validation error.
  - Smoke output: `final smoke ok {'low': 'InvoiceGenerated', 'paid': 'Paid', 'high_mid': 'ManagerApproval', 'high_done': 'InvoiceGenerated', 'resubmitted': 'AccountsReview', 'dashboard_total': 20}`.
- Cleanup:
  - Command: `docker compose down -v --remove-orphans`.
  - Result: FlowLedger containers, network, and SQL Server volume were removed.

### Notes

- No Browser tool was available in this session, so screenshots were documented as manual review targets rather than committed image files.
- No secrets were added to git-tracked files. README points to `.env.example` placeholders and environment variable names only.
- Per project instruction, changes are not committed yet. Commit only after explicit user signal.

## README AI skills note

### Built

- Updated the README AI tools section to list the project skills used during the build:
  - `caveman`
  - `ui-ux-pro-max`
  - `caveman-commit`

### Verification

- Docs-only change. No build or test run was needed.

### Notes

- Per project instruction, changes are not committed yet. Commit only after explicit user signal.

### Security hardening after review

- Reworked auth so app credentials are no longer hardcoded.
  - Added `PasswordHash` and `PasswordSalt` columns to `Users`.
  - Added PBKDF2 password hashing with per-password salt.
  - Login now verifies the supplied password against the database hash.
  - Seeded demo users have blank hashes in git-tracked seed data.
  - Local/demo hashes are bootstrapped only from environment values like `SeedUsers__SalesPassword`.
- Added `AppSettings` table.
  - Seeded `Jwt.AccessTokenMinutes = 30`.
  - JWT generation reads token expiry from the database setting.
- Added EF migration `AddAuthSecurityFields`.
- Separated test credentials from regular app seed data.
  - Unit and integration auth tests now use a test-only user row: `auth-test-sales@flowledger.local`.
  - Tests create the test user's password hash in the test database.
- Added `docs/backlog.md` with regular-priority item for admin-driven active session revocation.
- Updated `docs/agent-build-sessions/01-initial-flowledger-build/erp_workflow_build_plan.md`.
  - Removed plaintext demo password references.
  - Added DB-backed password hash guidance.
  - Added UI-phase instruction: expired JWT causing `401` must clear auth state, redirect to `/login`, and ask user to log in again.
- Updated `docs/deployment-security.md` and `.env.example` with seeded-user password environment variables.
- Updated `docker-compose.yml` so API receives `SeedUsers__*Password` environment values.

### Re-verification after hardening

- Passed: `dotnet restore backend/FlowLedger.sln`.
  - Result: all projects up to date.
- Passed: `dotnet build backend/FlowLedger.sln --no-restore`.
  - Result: build succeeded with 0 warnings and 0 errors.
- Initial test rerun after hardening failed once:
  - Command: `dotnet test backend/FlowLedger.sln --no-build`.
  - Result: 11 passed, 1 failed.
  - Cause: `IJwtTokenGenerator` was registered even when no database connection was configured, but JWT expiry now depends on database settings.
  - Fix: register `IJwtTokenGenerator` only when infrastructure/database services are registered.
- Passed after fix: `dotnet test backend/FlowLedger.sln --no-build`.
  - Result: 12 tests passed, 0 failed.
  - Coverage confirmed:
    - DB-backed password verification.
    - Wrong password rejection.
    - Test-only auth data separated from app seed users.
    - JWT contains required claims.
    - JWT expiry comes from `Jwt.AccessTokenMinutes` database setting.
    - Swagger bearer security scheme.
- Passed: `cd frontend/flowledger-web && npm run build`.
  - Result: TypeScript and Vite production build succeeded.
- Passed: Docker Compose clean-volume verification.
  - Command: generated temporary SQL/JWT secrets and temporary `SeedUsers__*Password` values in shell, then ran `docker compose up --build -d`.
  - First smoke attempt returned `401` for login.
  - Cause: `docker-compose.yml` did not pass `SeedUsers__*Password` values into the API container.
  - Fix: added those environment mappings to the API service.
  - Retest result: API `/health` returned `{ "status": "ok" }`.
  - Retest result: `POST /api/auth/login` with `sales@flowledger.local` and the generated `SeedUsers__SalesPassword` returned an access token.
  - Retest result: `GET /api/auth/me` with the returned token returned `sales@flowledger.local` and role `Sales`.
  - Retest result: token expiry decoded to 30 minutes from the database setting.
  - Retest result: frontend returned `HTTP/1.1 200 OK`.
  - Retest result: Swagger UI `Try it out` login returned `200` and an `accessToken` with 0 console errors.
  - Cleanup: `docker compose down -v --remove-orphans` stopped and removed containers, network, and SQL Server volume.

### Review feedback fixes

- Reworked app-setting reader API.
  - Replaced vague `GetIntAsync(...)` with `IAppSettingReader.ReadValueAsync(key, cancellationToken)`.
  - Kept one generic database-backed setting reader so future settings do not need one method each.
  - JWT token generation parses `Jwt.AccessTokenMinutes` at the call site.
- Updated auth service unit tests to use a shared DI fixture.
  - Tests now register `FlowLedgerDbContext`, `IAuthService`, `IPasswordHasher`, and fake `IJwtTokenGenerator` in `ServiceCollection`.
  - The fixture creates one service provider and scope for the class.
  - Tests resolve `IAuthService` from the fixture instead of manually constructing `AuthService`.
- Passed: `dotnet build backend/FlowLedger.sln --no-restore`.
  - Result: build succeeded with 0 warnings and 0 errors.
- Passed: `dotnet test backend/FlowLedger.sln --no-build`.
  - Result: 12 tests passed, 0 failed.

### Test container fixture optimization

- Replaced per-test SQL Server Testcontainers setup with class fixtures.
  - `DatabaseMigrationFixture` owns one SQL Server container for all database migration tests.
  - `AuthEndpointFixture` owns one SQL Server container and one `WebApplicationFactory` for all auth endpoint tests.
  - Auth endpoint fixture seeds the test-only user once per class fixture.
- Expected Docker behavior:
  - Integration tests now create one SQL Server container per integration test class, not one per test method.
  - Current backend test run uses 2 SQL Server containers for 8 SQL-backed tests instead of about 8 containers.
- Passed: `dotnet build backend/FlowLedger.sln --no-restore`.
  - Result: build succeeded with 0 warnings and 0 errors.
- Passed: `dotnet test backend/FlowLedger.sln --no-build`.
  - Result: 12 tests passed, 0 failed.
  - Runtime improved from roughly 35 seconds to roughly 10 seconds on this machine.

### Local deployment for manual Phase 3 review

- Created a local gitignored `.env` file with generated development-only values.
  - File permissions: owner read/write only.
  - Values are not tracked by git.
- Passed: `docker compose up --build -d`.
  - Result: SQL Server became healthy.
  - Result: API container started on `http://localhost:8080`.
  - Result: frontend container started on `http://localhost:5173`.
- Passed: local API smoke checks.
  - `GET http://localhost:8080/health` returned `{ "status": "ok" }`.
  - Swagger JSON contained bearer security scheme.
  - `POST http://localhost:8080/api/auth/login` with `sales@flowledger.local` and the local `.env` `SeedUsers__SalesPassword` returned an access token.
  - `GET http://localhost:8080/api/auth/me` with the returned token returned `sales@flowledger.local` and role `Sales`.
  - Token expiry decoded to 30 minutes from the database setting.
- Passed: frontend smoke check.
  - `curl -I http://localhost:5173` returned `HTTP/1.1 200 OK`.
- Current state:
  - Local Docker stack left running for manual review.

## Phase 4: Billing request APIs

### Built

- Added billing request application DTOs, query model, paged result model, and `IBillingRequestService`.
- Added current-user application model and API claims mapping from JWT claims.
- Added `BillingRequestService` with:
  - create draft request
  - list requests with status, customer, assigned-to-me, created-by-me, search, date, and paging filters
  - detail view with customer, line items, comments, audit logs, generated invoice, and available actions
  - update draft/rejected request
  - submit draft/rejected request to Accounts review
  - comments
  - Accounts approval
  - Manager approval
  - rejection back to Sales
  - invoice generation during approval
  - audit logs for create, update, submit, assignment, comment, approval, rejection, and invoice generation
- Added `BillingRequestsController`.
- Added `CustomersController` for the planned `GET /api/customers` create-form support endpoint.
- Reused role policies and enforced workflow rules in the service layer.
- Reworked endpoint test fixture to share one SQL Server container for API endpoint tests.

### Verification

- Local `dotnet` was not available in this shell. No global install was performed.
- Passed: backend build through Docker SDK container.
  - Command: `docker run --rm -v "$PWD:/src" -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet restore FlowLedger.sln && dotnet build FlowLedger.sln --no-restore'`.
  - Result: build succeeded with 0 warnings and 0 errors.
- Passed: Phase 4 filtered backend tests.
  - Command: `dotnet test FlowLedger.sln --no-build --filter "FullyQualifiedName~BillingRequest"` inside the SDK container with Testcontainers Docker socket access.
  - Result: 10 tests passed, 0 failed.
  - Coverage added:
    - service-level create calculates subtotal, VAT, total, and writes create audit log
    - service-level unauthorized Sales approval throws
    - API create/list/detail/submit flow
    - Accounts approval under threshold generates invoice and audit logs
    - Accounts approval above threshold moves request to manager approval
    - Manager approval generates invoice
    - Accounts rejection lets Sales update and resubmit
    - comments add comment and audit log
    - unauthorized Sales approval returns forbidden

### Notes

- Initial endpoint tests exposed EF Core tracking issues when adding new audit logs and line items with pre-set Guid IDs through unloaded navigation collections.
- Fix: add new audit logs and line items through `DbSet.Add(...)`.
- Phase 4 is implemented and verified.

## Phase 5: Invoice APIs

### Built

- Added invoice application DTOs, query model, and `IInvoiceService`.
- Added `InvoiceService` with:
  - invoice list with status, customer, search, and paging filters
  - invoice detail with customer and billing request summary
  - mark paid
  - billing request status update to `Paid`
  - payment audit log
- Added `InvoicesController`.
- Enforced Accounts/Admin-only payment marking.

### Verification

- Passed: Phase 5 filtered backend tests.
  - Command: `dotnet test FlowLedger.sln --no-build --filter "FullyQualifiedName~InvoiceEndpointTests"` inside the SDK container with Testcontainers Docker socket access.
  - Result: 3 tests passed, 0 failed.
  - Coverage added:
    - invoice list and detail return generated invoice data
    - Accounts can mark issued invoice paid
    - payment marking updates invoice status, billing request status, paid timestamp, and audit log
    - Sales cannot mark invoice paid

### Notes

- Phase 5 is implemented and verified.

## Phase 6: Dashboard APIs

### Built

- Added dashboard application DTOs and `IDashboardService`.
- Added `DashboardService` with:
  - summary cards
  - status breakdown
  - monthly invoice trend
  - aging buckets
  - recent activity
- Added `DashboardController`.

### Verification

- Passed: Phase 6 filtered backend tests.
  - Command: `dotnet test FlowLedger.sln --no-build --filter "FullyQualifiedName~DashboardEndpointTests"` inside the SDK container with Testcontainers Docker socket access.
  - Result: 1 test passed, 0 failed.
  - Coverage added:
    - dashboard returns summary card values, status breakdown, invoice trend, aging buckets, and recent activity
- Passed: full backend test suite.
  - Command: `docker run --rm -e TESTCONTAINERS_RYUK_DISABLED=true -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal -v "$PWD:/src" -v /var/run/docker.sock:/var/run/docker.sock -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet test FlowLedger.sln --logger "console;verbosity=minimal"'`.
  - Result: 25 tests passed, 0 failed.
- Passed: frontend production build.
  - Command: `cd frontend/flowledger-web && npm run build`.
  - Result: TypeScript and Vite production build succeeded.
- Passed: Docker Compose runtime smoke check.
  - Command: `docker compose up --build -d`.
  - Result: SQL Server became healthy, API started on `http://localhost:8080`, and frontend started on `http://localhost:5173`.
  - API smoke result: login as Sales, Accounts, and Admin using local `.env` secrets succeeded.
  - API smoke result: created a billing request, submitted it, approved it, generated an issued invoice, found it through invoice search, and loaded dashboard summary.
  - API smoke output: `smoke ok InvoiceGenerated Issued 1 18`.
  - Frontend smoke result: `curl -I http://localhost:5173` returned `HTTP/1.1 200 OK`.
  - Cleanup: `docker compose down -v --remove-orphans` stopped and removed containers, network, and SQL Server volume.

### Notes

- Initial dashboard test exposed an EF Core SQL translation issue from projecting `enum.ToString()` inside a grouped query.
- Fix: group in SQL, materialize rows, then convert enum values to strings in memory.
- Phase 6 is implemented and verified.
- Per project instruction, changes are not committed yet. Commit only after explicit user signal.

## Validation hardening after Phase 4-6 review

### Built

- Added FluentValidation to the Application layer.
- Added request validators for:
  - `LoginRequestDto`
  - `CreateBillingRequestDto`
  - `CreateBillingRequestLineItemDto`
  - `UpdateBillingRequestDto`
  - `ApproveBillingRequestDto`
  - `RejectBillingRequestDto`
  - `AddCommentDto`
- Added an async API validation filter that:
  - resolves matching `IValidator<T>` instances for action arguments
  - runs `ValidateAsync`
  - returns `400 Bad Request` with `ValidationProblemDetails`
  - prevents controller/service execution when request shape is invalid
- Removed duplicate manual request-shape validation from `BillingRequestService`.
- Kept business/workflow guards in services:
  - not-found checks
  - role checks
  - status-transition checks
  - customer existence checks
  - invoice payment-state checks

### Verification

- Passed: backend restore/build through Docker SDK container.
  - Command: `docker run --rm -v "$PWD:/src" -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet restore FlowLedger.sln && dotnet build FlowLedger.sln --no-restore'`.
  - Result: build succeeded with 0 warnings and 0 errors.
- Passed: full backend test suite.
  - Command: `docker run --rm -e TESTCONTAINERS_RYUK_DISABLED=true -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal -v "$PWD:/src" -v /var/run/docker.sock:/var/run/docker.sock -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet test FlowLedger.sln --logger "console;verbosity=minimal"'`.
  - Result: 28 tests passed, 0 failed.
  - Coverage added:
    - invalid login email shape returns `400 Bad Request`
    - invalid billing request create payload returns `400 Bad Request`
    - empty comment body returns `400 Bad Request`
- Passed: frontend production build.
  - Command: `cd frontend/flowledger-web && npm run build`.
  - Result: TypeScript and Vite production build succeeded.
- Passed: Docker Compose runtime smoke check after validation change.
  - Command: `docker compose up --build -d`.
  - Result: SQL Server became healthy, API started, and frontend started.
  - API smoke result: invalid billing request payload returned `400`.
  - API smoke result: login, create billing request, submit, approve, and invoice generation still worked.
  - API smoke output: `validation smoke ok InvoiceGenerated Issued`.
  - Cleanup: `docker compose down -v --remove-orphans` stopped and removed containers, network, and SQL Server volume.

### Notes

- Local `dotnet` was still unavailable in this shell. No global install was performed.
- `DbContext.Add(...)`, collection `.ToList()`, and property assignments are in-memory operations, not database I/O. Database reads and writes remain async (`ToListAsync`, `SingleOrDefaultAsync`, `CountAsync`, `SaveChangesAsync`, etc.).
- A full repository pattern was not added. The build plan explicitly says not to add a full repository pattern over EF Core unless necessary. EF Core `DbContext` is already the unit-of-work/repository abstraction for this scope.
- Per project instruction, changes are not committed yet. Commit only after explicit user signal.

## Dashboard period filter and query optimization

### Built

- Added `DashboardQuery` with `periodMonths`.
- Added FluentValidation for dashboard period values.
  - Allowed values: `1`, `3`, `6`, and `12`.
  - Default period: `1` month.
- Updated `GET /api/dashboard/summary` to accept `periodMonths` from query string.
- Refactored `DashboardService.GetSummaryAsync`.
  - Request reporting data is loaded as a bounded projection for the selected period.
  - Invoice reporting data is loaded as a bounded projection for the selected period.
  - Current pending request data is loaded separately so old/stale pending approvals are not hidden by the period filter.
  - Recent activity is filtered to the selected period and limited to 10 rows.
- Reduced repeated request/invoice database roundtrips by deriving multiple dashboard values from bounded projected rows in memory.
- Kept full entities and navigation-heavy loads out of dashboard summary calculations.

### Verification

- Passed: backend restore/build through Docker SDK container.
  - Command: `docker run --rm -v "$PWD:/src" -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet restore FlowLedger.sln && dotnet build FlowLedger.sln --no-restore'`.
  - Result: build succeeded with 0 warnings and 0 errors.
- Passed: full backend test suite.
  - Command: `docker run --rm -e TESTCONTAINERS_RYUK_DISABLED=true -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal -v "$PWD:/src" -v /var/run/docker.sock:/var/run/docker.sock -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet test FlowLedger.sln --logger "console;verbosity=minimal"'`.
  - Result: 29 tests passed, 0 failed.
  - Coverage added:
    - dashboard summary accepts `periodMonths=6` and returns seeded reporting data
    - invalid `periodMonths=2` returns `400 Bad Request`
- Passed: frontend production build.
  - Command: `cd frontend/flowledger-web && npm run build`.
  - Result: TypeScript and Vite production build succeeded.
- Passed: Docker Compose runtime smoke check.
  - Command: `docker compose up --build -d`.
  - Result: SQL Server became healthy, API started, and frontend started.
  - API smoke result: `GET /api/dashboard/summary?periodMonths=2` returned `400`.
  - API smoke result: default dashboard period returned current one-month reporting data.
  - API smoke result: `GET /api/dashboard/summary?periodMonths=6` returned seeded reporting data and current pending backlog.
  - API smoke output: `dashboard period smoke ok 0 17 3`.
  - Cleanup: `docker compose down -v --remove-orphans` stopped and removed containers, network, and SQL Server volume.

### Notes

- Default one-month reporting returns `0` seeded requests right now because seed data is from January 2026 and current date is June 2026. Current pending backlog remains visible.
- Per project instruction, changes are not committed yet. Commit only after explicit user signal.

## Phase 7: Frontend auth and layout

### Built

- Added the planned frontend stack dependencies for React Router, TanStack Query, Axios, React Hook Form, Zod, Tailwind CSS, shadcn-style UI primitives, Sonner, Lucide icons, Recharts, and frontend tests.
- Added Tailwind configuration and replaced the Vite starter page with the FlowLedger application shell.
- Added API client with JWT bearer token attachment.
- Added centralized `401` handling.
  - Stored auth is cleared.
  - User is redirected to `/login`.
  - Login page tells the user the session expired and asks them to log in again.
- Added `AuthProvider`, protected routes, login page, and app layout with sidebar/topbar navigation.
- Added role permission helpers for request creation, approval/rejection, submission/update, and invoice payment actions.
- Added a small configured CORS policy in the API so the Vite frontend at `http://localhost:5173` can call the API at `http://localhost:8080`.

### Verification

- Passed: frontend tests.
  - Command: `cd frontend/flowledger-web && npm test`.
  - Result: 5 tests passed, 0 failed.
  - Coverage added:
    - login form validation requires valid email and password
    - role permission helpers match planned Sales, Accounts, Manager, and Admin behavior
- Passed: frontend production build.
  - Command: `cd frontend/flowledger-web && npm run build`.
  - Result: TypeScript and Vite production build succeeded.
  - Note: Vite warned that one bundle chunk is over 500 kB after minification. This is acceptable for the current non-code-split assignment scope.
- Passed: backend build through Docker SDK container.
  - Command: `docker run --rm -v "$PWD:/src" -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet build FlowLedger.sln'`.
  - Result: build succeeded with 0 warnings and 0 errors.
- Passed: full backend test suite.
  - Command: `docker run --rm -e TESTCONTAINERS_RYUK_DISABLED=true -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal -v "$PWD:/src" -v /var/run/docker.sock:/var/run/docker.sock -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet test FlowLedger.sln --logger "console;verbosity=minimal"'`.
  - Result: 29 tests passed, 0 failed.
- Passed: frontend run smoke.
  - Command: `cd frontend/flowledger-web && npm run dev -- --host 0.0.0.0 --port 5173`.
  - Result: Vite started on `http://localhost:5173`.
  - Smoke command: `curl -I http://localhost:5173/login`.
  - Result: `HTTP/1.1 200 OK`.

### Notes

- Local `dotnet` is still unavailable in this shell. No global install was performed.
- Phase 7 routes intentionally use placeholders for app content. Phase 8 fills the dashboard, request, invoice, customer, and settings pages.
- Per project instruction, changes are not committed yet. Commit only after explicit user signal.

## Phase 8: Frontend pages

### Built

- Replaced Phase 7 placeholders with the planned FlowLedger pages:
  - Dashboard
  - Billing request list
  - Create billing request
  - Edit draft/rejected billing request
  - Billing request detail
  - Invoice list
  - Invoice detail
  - Customers
  - Settings/About
- Added frontend API modules for dashboard, customers, billing requests, and invoices.
- Added typed frontend DTOs matching API responses.
- Added shared UI components:
  - page header
  - metric card
  - status badge
  - loading/error/empty states
  - audit timeline
  - action dialog
  - shadcn-style table, select, textarea, skeleton, and dialog primitives
- Added Dashboard charts with Recharts:
  - status donut
  - invoice trend bar chart
  - aging bucket horizontal bar chart
  - recent activity timeline
- Added role-aware request list defaults:
  - Sales defaults to created-by-me
  - Accounts defaults to Accounts Review
  - Manager defaults to Manager Approval
  - Admin sees all by default
- Added request filters for search, status, customer, assigned-to-me, and created-by-me.
- Added invoice filters for search, status, and customer.
- Added create/edit billing request UI with React Hook Form and Zod.
  - customer required
  - title required, 3-200 characters
  - description max 2000 characters
  - at least one line item
  - line item description required
  - quantity 1-10,000
  - unit price 1-10,000,000
  - live subtotal, VAT, and total calculation
- Added end-user workflows in the UI:
  - Sales creates draft
  - Sales saves and submits
  - Accounts approves or rejects
  - Accounts approval under threshold generates invoice
  - Accounts approval above threshold moves request to Manager approval
  - Manager approves or rejects
  - Sales edits rejected request and resubmits
  - any authorized user adds comments
  - Accounts/Admin marks issued invoices paid
- Added invoice detail styled as a printable invoice with a print action.
- Added Settings/About page with architecture summary, demo user emails, password environment variable names, known limitations, and AI usage note.

### Verification

- Passed: frontend tests.
  - Command: `cd frontend/flowledger-web && npm test`.
  - Result: 9 tests passed, 0 failed.
  - Coverage added:
    - login form validation
    - role permission helpers
    - API client clears stored auth and calls unauthorized handler on `401`
    - billing request form validation
    - billing request subtotal/VAT/total calculation
- Passed: frontend lint.
  - Command: `cd frontend/flowledger-web && npm run lint`.
  - Result: ESLint passed with 0 errors.
- Passed: frontend production build.
  - Command: `cd frontend/flowledger-web && npm run build`.
  - Result: TypeScript and Vite production build succeeded.
  - Note: Vite warned that the main bundle is over 500 kB after minification because current Phase 8 keeps all pages/charts in one bundle. This is acceptable for the current assignment scope; route-level code splitting can be added later if needed.
- Passed: backend build through Docker SDK container.
  - Command: `docker run --rm -v "$PWD:/src" -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet build FlowLedger.sln'`.
  - Result: build succeeded with 0 warnings and 0 errors.
- Passed: full backend test suite.
  - Command: `docker run --rm -e TESTCONTAINERS_RYUK_DISABLED=true -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal -v "$PWD:/src" -v /var/run/docker.sock:/var/run/docker.sock -w /src/backend mcr.microsoft.com/dotnet/sdk:8.0-alpine sh -lc 'dotnet test FlowLedger.sln --logger "console;verbosity=minimal"'`.
  - Result: 29 tests passed, 0 failed.
- Passed: Docker Compose build and runtime smoke.
  - First Compose attempt exposed `package-lock.json` mismatch with Dockerfile `npm ci`.
  - Fix command: `docker run --rm -v "$PWD:/app" -w /app node:22-alpine npm install --package-lock-only` from `frontend/flowledger-web`.
  - Re-run command: `docker compose up --build -d`.
  - Result: SQL Server became healthy, API started, and frontend started.
  - Frontend smoke:
    - `curl -I http://localhost:5173/login` returned `200`.
    - `curl -I http://localhost:5173/app/dashboard` returned `200`.
  - API CORS smoke:
    - `OPTIONS http://localhost:8080/api/dashboard/summary` with `Origin: http://localhost:5173` returned `204`.
  - API workflow smoke:
    - Sales login succeeded.
    - Accounts login succeeded.
    - Manager login succeeded.
    - Admin login succeeded.
    - Sales created and submitted a low-value request.
    - Accounts approved it and invoice was generated.
    - Accounts marked the invoice paid.
    - Sales created and submitted a high-value request.
    - Accounts approved it and it moved to `ManagerApproval`.
    - Manager approved it and invoice was generated.
    - Sales created and submitted a request.
    - Accounts rejected it.
    - Sales revised it and resubmitted it to `AccountsReview`.
  - Smoke output: `phase8 smoke ok low=InvoiceGenerated paid=Paid high_escalated=ManagerApproval high_final=InvoiceGenerated rejected=Rejected resubmitted=AccountsReview dashboard_total=3 cors=204`.
  - Cleanup: `docker compose down -v --remove-orphans` stopped and removed containers, network, and SQL Server volume.
- Passed: frontend Docker image rebuild after adding `.dockerignore`.
  - Command: `docker compose build web`.
  - Result: image build succeeded and frontend production build ran inside Docker.

### Notes

- UI required API CORS support because browser requests come from `http://localhost:5173` while the API runs on `http://localhost:8080`. Added a configured CORS policy with local Vite origin as default.
- The edit UI uses `/app/requests/:id/edit` to support the planned rejected-request revision flow. Backend already supported update and resubmit.
- No browser automation dependency was added. Runtime UI verification used Vite/Docker build, frontend route responses, TypeScript, lint, tests, and API workflow smoke.
- Local `dotnet` is still unavailable in this shell. No global install was performed.
- Per project instruction, changes are not committed yet. Commit only after explicit user signal.

## Session 01 sign-off documentation refactor

### Built

- Moved session-specific build artifacts under `docs/agent-build-sessions/01-initial-flowledger-build/`.
  - `erp_workflow_build_plan.md`
  - `implementation-log.md`
- Added `session-flow.svg` and generated `session-flow.png` in the same session directory to show the current software behavior and user flow after this session.
- Updated project references from the former root-level plan/log paths to the new session directory.
- Updated `README.md` with an Agent build sessions section and embedded the Session 01 behaviour flow image.
- Expanded the README AI section with the session hygiene technique:
  - numbered build-session directories
  - per-session plan and implementation log
  - sign-off review of the latest log changes
  - README updates when project behaviour/setup changes
  - generated behaviour-flow image for each session
- Updated `AGENTS.md` so future agents:
  - use numbered session directories
  - read the active implementation log from the bottom first
  - review the log at sign-off for README updates
  - generate or refresh `session-flow.png` after each signed-off session

### Verification

- Passed: SVG to PNG conversion.
  - Command: `sips -s format png docs/agent-build-sessions/01-initial-flowledger-build/session-flow.svg --out docs/agent-build-sessions/01-initial-flowledger-build/session-flow.png`.
  - Result: PNG generated successfully.
- Passed: visual inspection of `session-flow.png`.
  - Result: flowchart shows Sales, Accounts, Manager, and System lanes with submit, approval, rejection, invoice, payment, dashboard, and audit behavior.
- Passed: old prompt-for-user section remains removed from the build plan.
- Docs-only/session-organization change. No app build or test run was needed.

### Notes

- This sign-off entry is intentionally at the bottom of the log so the next session can read bottom-first and understand the latest project state quickly.
- Per project instruction, changes are not committed yet. Commit only after explicit user signal.
