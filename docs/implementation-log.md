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
