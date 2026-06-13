# FlowLedger UI Smoothness + Dashboard Density Plan

## Summary

Improve perceived UI responsiveness, add a first-load Work Queue badge, enrich recent dashboard demo data, and verify with focused frontend/backend tests plus runtime smoke checks.

This build session is internal evidence. Reader-facing README text and the behavior-flow image should describe the product, not this session number.

## Key Changes

- Add subtle app-content route transitions with reduced-motion support.
- Animate shared dialogs on open/close without layout-shifting hover or scale effects elsewhere.
- Add a role-aware Work Queue badge beside `My Work`.
  - Count comes from existing `GET /api/work-queue?pageSize=1`.
  - Badge stays visible while role-specific work remains pending.
  - Badge disappears only when queue count reaches zero after work is actually attended.
- Hold `GET` request completion for one second in frontend runtime so loading states remain briefly visible even when local APIs respond immediately.
- Add modest demo rows across 1, 3, 6, and 12 month windows through the non-production demo data refresher, not EF static migration seed.
- Update README with reader-facing capability notes only.
- Add `session-flow.svg` and generated `session-flow.png` at sign-off with a generic title.

## Test Plan

- Backend:
  - Dashboard default 1-month summary has seeded activity.
  - 1, 3, 6, and 12 month period metrics vary as expected.
  - Current workload metrics remain invariant across period filters.
  - Full backend test suite passes through Docker SDK/Testcontainers.
- Frontend:
  - Work Queue badge renders from `totalCount`, remains visible after `My Work` click if work still exists, reappears on remount, and stays hidden for zero count.
  - App route transition renders routed children.
  - Dialog render/close behavior remains stable with transition classes.
  - Frontend API client keeps `GET` loaders visible for one second.
  - `npm test`, `npm run lint`, and `npm run build` pass.
- Runtime:
  - Docker Compose starts API/frontend/SQL Server.
  - `/health` and frontend return `200`.
  - Dashboard 1/3/6/12 filters show different period values.
  - Work Queue count endpoint stays aligned with role-specific queue state.
  - Cleanup with `docker compose down -v --remove-orphans`.
