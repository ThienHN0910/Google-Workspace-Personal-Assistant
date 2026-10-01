# Background Jobs Monitoring & Management Dashboard Design

- **Date:** 2026-10-01
- **Status:** Approved
- **Branch:** `feat/background-jobs-dashboard`

---

## 1. Context & Motivation

G-Ops Hub relies on four core background automated recurring jobs powered by Hangfire and MongoDB:
1. **Drive Guard Audit (`drive-guard-audit`)**: Scans monitored Google Drive folders for file changes, suspicious executable files, and bulk file deletion anomalies (UC05 & UC06).
2. **Bank Telemetry & Sheets Sync (`bank-telemetry`)**: Scans bank transaction alert emails, extracts transaction details with Gemini AI, and syncs records into Google Sheets (UC04).
3. **Automated Inbox Cleanup (`email-cleanup`)**: Identifies junk/promotional emails based on approved regex rules and user feedback preferences, safely moving them to trash while protecting important and starred emails (UC01).
4. **Smart Calendar Schedule Extractor (`calendar-extractor`)**: Scans incoming appointment/meeting emails and generates pending calendar events for human approval (UC03).

### Audit Findings
- **Feature Integrity**: All 4 recurring job classes ([`DriveGuardBackgroundJob.cs`](file:///E:/workspace/srcPrj/Google-Workspace-Personal-Assistant/src/backend/GOpsHub.Application/Features/DriveGuard/DriveGuardBackgroundJob.cs), [`BankTelemetryBackgroundJob.cs`](file:///E:/workspace/srcPrj/Google-Workspace-Personal-Assistant/src/backend/GOpsHub.Application/Features/Finance/BankTelemetryBackgroundJob.cs), [`EmailCleanupBackgroundJob.cs`](file:///E:/workspace/srcPrj/Google-Workspace-Personal-Assistant/src/backend/GOpsHub.Application/Features/EmailOps/EmailCleanupBackgroundJob.cs), [`CalendarScheduleBackgroundJob.cs`](file:///E:/workspace/srcPrj/Google-Workspace-Personal-Assistant/src/backend/GOpsHub.Application/Features/Scheduling/CalendarScheduleBackgroundJob.cs)) remain completely intact, registered in Dependency Injection, and registered with Hangfire `IRecurringJobManager` during startup and system settings updates.
- **Routing Issue**: In [`BackgroundJobsController.cs`](file:///E:/workspace/srcPrj/Google-Workspace-Personal-Assistant/src/backend/GOpsHub.API/Controllers/BackgroundJobsController.cs), the route attribute was previously only `[Route("api/v1/[controller]")]` (`/api/v1/BackgroundJobs`), whereas frontend callers used `/api/v1/jobs`. This caused a 404 error when triggering or querying jobs from the frontend.
- **Observability Gap**: Users previously had only a small dashboard summary widget showing upcoming schedules, but lacked a dedicated page to inspect completed background executions, failure logs, and execution durations.

---

## 2. Requirements & Goals

1. **Fix API Routing**: Ensure both `/api/v1/jobs` and `/api/v1/backgroundjobs` routes resolve cleanly.
2. **Dedicated Background Jobs View**: Create a new frontend view `/background-jobs` with two primary tabs:
   - **Tab 1: Upcoming Tasks (Lịch trình sắp tới)**: Real-time recurring schedules, cron intervals, last run timestamps, next run countdowns, and immediate trigger actions.
   - **Tab 2: Execution History (Lịch sử thực thi)**: Detailed log of past executions (Succeeded, Failed, Processing) including execution start time, duration, status, and expandable stack trace/error details for failures.
3. **Navigation & Dashboard Integration**:
   - Add "Tác vụ chạy ngầm" with `pi pi-server` icon to the primary sidebar in [`DefaultLayout.vue`](file:///E:/workspace/srcPrj/Google-Workspace-Personal-Assistant/src/frontend/src/layouts/DefaultLayout.vue).
   - Add a quick navigation button in [`BackgroundJobsPanel.vue`](file:///E:/workspace/srcPrj/Google-Workspace-Personal-Assistant/src/frontend/src/components/common/BackgroundJobsPanel.vue) linking directly to `/background-jobs`.

---

## 3. Architecture & Technical Specification

### 3.1. Backend API (`BackgroundJobsController.cs`)

#### Routes & Attributes
- Add `[Route("api/v1/jobs")]` alongside `[Route("api/v1/[controller]")]`.
- Retain `[Authorize]` attribute to enforce JWT authentication.

#### DTOs
```csharp
public class JobInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Cron { get; set; } = string.Empty;
    public DateTime? NextExecution { get; set; }
    public DateTime? LastExecution { get; set; }
    public string LastJobState { get; set; } = string.Empty;
}

public class JobExecutionHistoryDto
{
    public string JobId { get; set; } = string.Empty;
    public string JobKey { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string State { get; set; } = "Succeeded"; // Succeeded, Failed, Processing
    public DateTime? ExecutedAt { get; set; }
    public long? DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExceptionDetails { get; set; }
}
```

#### Endpoints
1. `GET /api/v1/jobs`
   - Retrieves active recurring jobs via `JobStorage.Current.GetConnection().GetRecurringJobs()`.
   - Populates metadata (Friendly name, description, next/last execution).
2. `GET /api/v1/jobs/history?limit=50`
   - Retrieves historical executions via `JobStorage.Current.GetMonitoringApi()`.
   - Aggregates `SucceededJobs(0, limit)` and `FailedJobs(0, limit)` and `ProcessingJobs(0, limit)`.
   - Maps method calls (`DriveGuardBackgroundJob.RunAuditAsync`, `EmailCleanupBackgroundJob.RunAutoCleanupAsync`, `BankTelemetryBackgroundJob.RunTelemetryAsync`, `CalendarScheduleBackgroundJob.RunScheduleExtractionAsync`) to user-friendly titles and job keys.
   - Orders descending by `ExecutedAt`.
3. `POST /api/v1/jobs/{id}/trigger`
   - Invokes `IRecurringJobManager.Trigger(id)`.

---

### 3.2. Frontend Implementation

#### 1. Router (`src/frontend/src/router/index.ts`)
- Path: `background-jobs`
- Name: `BackgroundJobs`
- Meta: `{ title: 'Tác vụ chạy ngầm', requiresAuth: true }`
- Component: `() => import('@/views/BackgroundJobsView.vue')`

#### 2. Layout Navigation (`src/frontend/src/layouts/DefaultLayout.vue`)
- Insert `<router-link to="/background-jobs" class="nav-item">` with icon `<i class="pi pi-server"></i>` and label `Tác vụ chạy ngầm (Jobs)`.

#### 3. New View (`src/frontend/src/views/BackgroundJobsView.vue`)
- **Header**: Status badges (Engine Online, Hangfire Worker active, Last refreshed time), "Làm mới" button.
- **Tabs**:
  - `upcoming`: 4 recurring job cards with responsive design, icon badges, next execution countdown, last execution, and action buttons.
  - `history`:
    - Status filter pills: "Tất cả", "Thành công" (green), "Thất bại" (red).
    - Table columns: Tác vụ (Job Name & Key), Trạng thái (Status Pill), Thời điểm chạy (Formatted datetime), Thời lượng (Duration in ms/seconds), Thao tác (Xem log lỗi nếu có).
    - Modal dialog to view formatted exception stack trace when a failed job row is clicked.

#### 4. Dashboard Integration (`src/frontend/src/components/common/BackgroundJobsPanel.vue`)
- Add button: `<router-link to="/background-jobs" class="btn-view-all"><i class="pi pi-external-link"></i> Xem lịch sử & chi tiết</router-link>`.

---

## 4. Verification & Testing Plan

1. **Backend Unit Tests**:
   - Create unit tests verifying `BackgroundJobsController` returns recurring jobs and formatted history.
   - Verify fallback handling if Hangfire storage connection throws transient exceptions.
   - Run `dotnet test src/backend/GOpsHub.sln` to ensure 100% pass rate.
2. **Frontend Typecheck & Build**:
   - Run `vue-tsc --noEmit` and `vite build` in `src/frontend` to verify zero compile or styling errors.
3. **Secrets Hygiene**:
   - Verify no credentials or `.env` files staged.
