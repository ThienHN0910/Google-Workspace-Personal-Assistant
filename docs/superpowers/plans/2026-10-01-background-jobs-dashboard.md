# Background Jobs Monitoring & Management Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Provide full observability and control of background recurring jobs by updating all 4 default intervals to 2 hours, fixing backend route aliases, implementing an execution history monitoring API endpoint, and building a dedicated 2-tab Background Jobs page in Vue 3.

**Architecture:** ASP.NET Core `BackgroundJobsController` queries Hangfire's `IRecurringJobManager` for recurring schedules and `JobStorage.Current.GetMonitoringApi()` for real-time execution history. The frontend uses a Vue 3 component with tabbed navigation (Upcoming Tasks & Execution History) integrated into the sidebar navigation and dashboard widget.

**Tech Stack:** ASP.NET Core 8, Hangfire, MongoDB, Vue 3, TypeScript, PrimeIcons, Pinia.

**Spec:** `docs/superpowers/specs/2026-10-01-background-jobs-dashboard-design.md`

## Global Constraints

- Never hardcode credentials; environment variables only.
- Strict Conventional Commits (`feat:`, `fix:`, `refactor:`, `test:`, `chore:`).
- Zero-error local verification (typecheck, unit tests, frontend build) before task completion.
- All 4 background cycles default to 2 hours (120 minutes for DriveGuard and BankTelemetry; 2 hours for EmailCleanup and CalendarExtractor).

---

### Task 1: Update Default Intervals to 2 Hours Across Backend, Frontend & Tests

**Files:**
- Modify: `src/backend/GOpsHub.API/Program.cs`
- Modify: `src/backend/GOpsHub.Application/Features/Settings/SettingsCommandsAndQueries.cs`
- Modify: `src/frontend/src/views/SettingsView.vue`
- Test: `src/backend/GOpsHub.Tests/Unit/SettingsCommandHandlerTests.cs`

**Interfaces:**
- `SystemSettingsDto.DriveGuardIntervalMinutes` = 120
- `SystemSettingsDto.BankTelemetryIntervalMinutes` = 120
- `SystemSettingsDto.EmailCleanupIntervalHours` = 2
- `SystemSettingsDto.CalendarExtractorIntervalHours` = 2

- [ ] **Step 1: Update defaults in `SettingsCommandsAndQueries.cs` and `Program.cs`**
- [ ] **Step 2: Update default form values in `SettingsView.vue`**
- [ ] **Step 3: Update and verify unit tests in `SettingsCommandHandlerTests.cs`**
- [ ] **Step 4: Run `dotnet test src/backend/GOpsHub.sln` to confirm tests pass**
- [ ] **Step 5: Commit changes**

---

### Task 2: Backend Route Fix and Execution History Monitoring API

**Files:**
- Modify: `src/backend/GOpsHub.API/Controllers/BackgroundJobsController.cs`
- Test: `src/backend/GOpsHub.Tests/Unit/BackgroundJobsControllerTests.cs`

**Interfaces:**
- Consumes: Hangfire `JobStorage.Current.GetMonitoringApi()`, `IRecurringJobManager`
- Produces:
  - `GET /api/v1/jobs` (returns `ApiResponse<List<JobInfoDto>>`)
  - `GET /api/v1/jobs/history?limit=50` (returns `ApiResponse<List<JobExecutionHistoryDto>>`)
  - `POST /api/v1/jobs/{id}/trigger` (returns `ApiResponse<bool>`)
  - Routes: `[Route("api/v1/jobs")]` and `[Route("api/v1/[controller]")]`

- [ ] **Step 1: Add unit tests for `BackgroundJobsController` in `GOpsHub.Tests`**
- [ ] **Step 2: Add `JobExecutionHistoryDto` and history endpoint with job name mapping & error parsing in `BackgroundJobsController.cs`**
- [ ] **Step 3: Add `[Route("api/v1/jobs")]` route alias to `BackgroundJobsController.cs`**
- [ ] **Step 4: Run `dotnet test src/backend/GOpsHub.sln` to verify all tests pass**
- [ ] **Step 5: Commit changes**

---

### Task 3: Frontend Dedicated Page (`BackgroundJobsView.vue`) & Layout Navigation

**Files:**
- Create: `src/frontend/src/views/BackgroundJobsView.vue`
- Modify: `src/frontend/src/router/index.ts`
- Modify: `src/frontend/src/layouts/DefaultLayout.vue`
- Modify: `src/frontend/src/components/common/BackgroundJobsPanel.vue`

**Interfaces:**
- Route: `/background-jobs`
- Sidebar: Item "Tác vụ chạy ngầm" with `pi pi-server`
- View: Tab 1 "Lịch trình sắp tới" (Upcoming), Tab 2 "Lịch sử thực thi" (Execution History with Succeeded/Failed filter and error details modal)
- Panel: Link button leading to `/background-jobs`

- [ ] **Step 1: Create `BackgroundJobsView.vue` with Upcoming and Execution History tabs**
- [ ] **Step 2: Register route in `src/frontend/src/router/index.ts`**
- [ ] **Step 3: Add menu link in `src/frontend/src/layouts/DefaultLayout.vue`**
- [ ] **Step 4: Add direct navigation link in `src/frontend/src/components/common/BackgroundJobsPanel.vue`**
- [ ] **Step 5: Run `npm run build` in `src/frontend` to verify clean build & typecheck**
- [ ] **Step 6: Commit changes**

---

### Task 4: End-to-End Verification & Polish

**Files:**
- All touched files
- Verification across backend test suite and frontend production build

- [ ] **Step 1: Run full backend test suite: `dotnet test src/backend/GOpsHub.sln`**
- [ ] **Step 2: Run frontend production build: `npm run build` in `src/frontend`**
- [ ] **Step 3: Verify git status and secrets hygiene**
- [ ] **Step 4: Final commit and summary report**
