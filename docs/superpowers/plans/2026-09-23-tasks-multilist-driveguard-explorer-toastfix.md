# Tasks Multi-List Board, Drive Guard Deep Explorer & Toast Auto-Dismiss Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix notification toast auto-dismiss (4s default), introduce a Multi-List Board view on the Tasks page to display multiple task lists side-by-side, and implement a deep Folder Explorer with breadcrumbs and file inspection in Drive Guard.

**Architecture:**
1. Frontend `App.vue` & `notification.service.ts`: Set fallback `life: payload.life ?? 4000` so non-critical toasts always dismiss automatically after 4 seconds.
2. Backend DriveGuard: Add CQRS `GetFolderContentsQuery` and `GET /api/v1/driveguard/folders/{folderId}/contents` utilizing existing `IDriveService.ListFilesInFolderAsync`.
3. Frontend DriveGuard: Add an interactive Folder Explorer with clickable breadcrumbs, folder drill-down, file size formatting, author info, and quick actions.
4. Frontend Tasks: Add a View Mode toggle (Single List vs Multi-List Board) allowing users to see multiple Google Tasks lists side-by-side in responsive Cyber-Cockpit columns with task creation and management per list.

**Tech Stack:** C# .NET 8 (CQRS, MediatR/Dispatcher pattern), Vue 3, TypeScript, PrimeVue Toast, SCSS/CSS Cyber-Cockpit styling.

---

### Task 1: Fix Toast Notification Auto-Dismiss (4s Default)

**Files:**
- Modify: `src/frontend/src/App.vue`
- Modify: `src/frontend/src/services/notification.service.ts`

- [x] **Step 1: Update `App.vue` to set `life: payload.life ?? 4000`**
- [x] **Step 2: Update `notification.service.ts` default payload handling**
- [x] **Step 3: Test and commit changes**

---

### Task 2: Backend Drive Guard - Folder Contents API Endpoint

**Files:**
- Create: `src/backend/GOpsHub.Application/Features/DriveGuard/Queries/GetFolderContentsQuery.cs`
- Modify: `src/backend/GOpsHub.API/Controllers/DriveGuardController.cs`
- Test: `src/backend/GOpsHub.Tests/Unit/DriveGuardFolderContentsTests.cs`

- [x] **Step 1: Write failing unit test for `GetFolderContentsQueryHandler` in `GOpsHub.Tests`**
- [x] **Step 2: Run test to verify failure**
- [x] **Step 3: Implement `GetFolderContentsQuery` and handler calling `IDriveService.ListFilesInFolderAsync`**
- [x] **Step 4: Add `[HttpGet("folders/{folderId}/contents")]` in `DriveGuardController.cs`**
- [x] **Step 5: Run tests to verify they pass**
- [x] **Step 6: Commit backend changes**

---

### Task 3: Frontend Drive Guard - Breadcrumb Navigation & Folder Deep-Dive Explorer

**Files:**
- Modify: `src/frontend/src/views/DriveGuardView.vue`

- [x] **Step 1: Add state for folder exploration (activeFolder, folderStack/breadcrumbs, folderItems, loadingContents)**
- [x] **Step 2: Implement `exploreFolder(folderId, folderName)` and `navigateToBreadcrumb(index)`**
- [x] **Step 3: Add UI section for Folder Explorer with Cyber-Cockpit bento styling, folder/file icons, file size, last modified, author**
- [x] **Step 4: Test frontend build with `npm run build`**
- [x] **Step 5: Commit changes**

---

### Task 4: Frontend Tasks - Multi-List Board View

**Files:**
- Modify: `src/frontend/src/views/TasksView.vue`

- [x] **Step 1: Add `viewMode` toggle state (`'single' | 'board'`) and `selectedBoardListIds`**
- [x] **Step 2: Implement multi-list task loading so tasks for all selected lists are fetched and indexed**
- [x] **Step 3: Add board layout template with side-by-side list columns, list headers, task count badges, and add task trigger**
- [x] **Step 4: Ensure filter tabs (All, Today, Starred, Completed) work seamlessly across board columns**
- [x] **Step 5: Test frontend build with `npm run build`**
- [x] **Step 6: Commit changes**

---

### Task 5: Local Verification & Integration

- [x] **Step 1: Run `dotnet test src/backend/GOpsHub.sln`**
- [x] **Step 2: Run `npm run build` in `src/frontend`**
- [x] **Step 3: Scan git diff for zero-leakage secrets hygiene and review branch status**
