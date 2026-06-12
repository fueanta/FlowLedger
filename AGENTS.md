# Repository Guidelines

## Project Structure & Module Organization

This repository currently contains the build plan in `docs/erp_workflow_build_plan.md`. FlowLedger is a full-stack billing approval and invoice workflow.

Expected layout:

```text
backend/                 .NET solution and API projects
  FlowLedger.Api/         Controllers, auth, Swagger, DI, middleware
  FlowLedger.Application/ DTOs, services, validators, use cases
  FlowLedger.Domain/      Entities, enums, domain rules
  FlowLedger.Infrastructure/ EF Core, SQL Server persistence, seed data
  FlowLedger.Tests/       xUnit unit and integration tests
frontend/flowledger-web/  React + Vite + TypeScript UI
docs/                     Design notes, API notes, screenshots
docker-compose.yml        Local SQL Server, API, and web stack
```

## Build, Test, and Development Commands

Use these commands once the planned projects are scaffolded:

```bash
docker compose up --build
```

Starts SQL Server, the ASP.NET Core API, and the React web app.

```bash
cd backend && dotnet build && dotnet test
```

Builds the .NET solution and runs backend tests.

```bash
cd frontend/flowledger-web && npm install && npm run dev
```

Installs frontend dependencies and starts Vite locally.

```bash
cd frontend/flowledger-web && npm run build
```

Produces the production frontend bundle.

## Coding Style & Naming Conventions

Use C#/.NET conventions in `backend/`: PascalCase for types and public members, camelCase for locals and parameters, async methods ending in `Async`, and one main type per file. Keep workflow rules out of controllers.

Use TypeScript in `frontend/flowledger-web`: PascalCase React components, camelCase hooks and utilities, and colocated UI code where practical. Prefer shadcn/ui, Tailwind, Lucide icons, TanStack Query, React Hook Form, and Zod.

## Testing Guidelines

Backend tests are required for workflow rules, role authorization, submission, approvals, invoice generation, and payment marking. Use xUnit, FluentAssertions, WebApplicationFactory, and Testcontainers for SQL Server-backed integration tests. Name tests by behavior, for example `Approve_UnderThreshold_GeneratesInvoice`.

Frontend tests are optional in the current plan. If added, use Vitest and focus on form validation, role-specific rendering, and critical workflow actions.

## Delivery Process

Follow `docs/erp_workflow_build_plan.md` exactly and implement one phase at a time. Do not over-engineer or add features outside the plan unless explicitly requested. Keep the code clean, simple, readable, and aligned with SOLID, KISS, DRY, and YAGNI.

Use caveman communication mode for assistant responses in this project unless the user says `stop caveman` or `normal mode`. Keep technical terms exact, drop filler, stay terse, and temporarily return to normal clarity for security warnings, irreversible action confirmations, or sequences where compression could cause ambiguity.

Use the local `ui-ux-pro-max` skill for UI/UX design, build, review, fix, or improvement work in this project. For UI work, start with its required design-system search, persist recommendations when useful, then apply its stack and UX guidance alongside the FlowLedger build plan.

After each phase, make sure the relevant app surfaces build and run before moving on. Write unit, integration, and end-to-end tests as the implemented phase requires, and do not ignore failing tests. Maintain a separate implementation log that records what was built, what build/test/run commands were executed, whether they succeeded, and any environment blockers.

When reading `docs/implementation-log.md`, read from the bottom first so the latest phase status, verification notes, and blockers are understood without scanning the full history.

If a required global system tool is missing, such as Docker, do not silently skip it or silently install it. Ask the user whether to install the tool or proceed with a clearly documented partial verification. Installing project-local dependencies is acceptable; installing machine-wide tools requires explicit user approval.

When a phase is confirmed working, prepare a clear Conventional Commit message and summarize the verified changes, but do not commit or push until the user explicitly says to commit.

## Commit & Pull Request Guidelines

This checkout has no local Git history, so no existing convention can be inferred. Use short imperative subjects, preferably Conventional Commits such as `feat: add billing request approval`.

Pull requests should include a concise summary, test evidence (`dotnet test`, `npm run build`, or Docker run result), linked issue when available, and screenshots for UI changes.

## Security & Configuration Tips

Keep secrets out of source. Use `.env.example` for documented local settings. Development JWT keys and SQL Server passwords in docs or Compose files must remain clearly marked as local-only values.
