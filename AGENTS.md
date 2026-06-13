# Repository Guidelines

## Project Structure & Module Organization

FlowLedger is a full-stack billing approval and invoice workflow. Session build plans and logs live under `docs/agent-build-sessions/`.

Expected layout:

```text
backend/                 .NET solution and API projects
  FlowLedger.Api/         Controllers, auth, Swagger, DI, middleware
  FlowLedger.Application/ DTOs, services, validators, use cases
  FlowLedger.Domain/      Entities, enums, domain rules
  FlowLedger.Infrastructure/ EF Core, SQL Server persistence, seed data
  FlowLedger.Tests/       xUnit unit and integration tests
frontend/flowledger-web/  React + Vite + TypeScript UI
docs/                     General docs, security notes, backlog
docs/agent-build-sessions/
  01-initial-flowledger-build/
    erp_workflow_build_plan.md
    implementation-log.md
    session-flow.png
docker-compose.yml        Local SQL Server, API, and web stack
```

## Build, Test, and Development Commands

Use these commands for local development:

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

Follow the active session build plan exactly and implement one phase at a time. Current session plan: `docs/agent-build-sessions/01-initial-flowledger-build/erp_workflow_build_plan.md`. Future sessions should live under `docs/agent-build-sessions/NN-short-session-name/`. Do not over-engineer or add features outside the plan unless explicitly requested. Keep the code clean, simple, readable, and aligned with SOLID, KISS, DRY, and YAGNI.

Use caveman communication mode for assistant responses in this project unless the user says `stop caveman` or `normal mode`. Keep technical terms exact, drop filler, stay terse, and temporarily return to normal clarity for security warnings, irreversible action confirmations, or sequences where compression could cause ambiguity.

Use the local `ui-ux-pro-max` skill for UI/UX design, build, review, fix, or improvement work in this project. For UI work, start with its required design-system search, persist recommendations when useful, then apply its stack and UX guidance alongside the FlowLedger build plan.

After each phase, make sure the relevant app surfaces build and run before moving on. Write unit, integration, and end-to-end tests as the implemented phase requires, and do not ignore failing tests. Maintain the active session implementation log that records what was built, what build/test/run commands were executed, whether they succeeded, and any environment blockers.

When reading the active session `implementation-log.md`, read from the bottom first so the latest phase status, verification notes, and blockers are understood without scanning the full history.

At session sign-off, review the active implementation log from the bottom, identify significant product, architecture, security, setup, or testing changes, and update `README.md` if those changes affect how reviewers understand or run the project.

At session sign-off, generate or update `session-flow.png` inside that session directory. The image should show the software behavior and user flow as it exists after the session. Keep an editable source beside it when practical, for example `session-flow.svg`.

If a required global system tool is missing, such as Docker, do not silently skip it or silently install it. Ask the user whether to install the tool or proceed with a clearly documented partial verification. Installing project-local dependencies is acceptable; installing machine-wide tools requires explicit user approval.

When a phase is confirmed working, prepare a clear Conventional Commit message and summarize the verified changes, but do not commit or push until the user explicitly says to commit.

## Commit & Pull Request Guidelines

Use short imperative subjects, preferably Conventional Commits such as `feat: add billing request approval`.

Pull requests should include a concise summary, test evidence (`dotnet test`, `npm run build`, or Docker run result), linked issue when available, and screenshots for UI changes.

## Security & Configuration Tips

Keep secrets out of source. Use `.env.example` for documented local settings. Development JWT keys and SQL Server passwords in docs or Compose files must remain clearly marked as local-only values.
