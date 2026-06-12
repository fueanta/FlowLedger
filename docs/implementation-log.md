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
- Next phase from `docs/erp_workflow_build_plan.md`: Phase 2, Backend domain and database.

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
