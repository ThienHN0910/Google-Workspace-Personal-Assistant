# Email Cleanup Preferences Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace self-confident cleanup and misleading AI training copy with safe Trash/Review/Keep decisions backed by durable user reasons and approved regex rules.

**Architecture:** Keep approved regex rules as the fast path, but place review, Keep, and sender safety gates before it. Use a separate `CleanupReview` state record, extend `CleanupFeedback` to positive and negative preferences, and make Gemini return per-email decisions that the server independently validates. Audit live rules with an idempotent dry-run migration before enabling the new behavior.

**Tech Stack:** .NET 8, MongoDB.Driver, Gmail API, Gemini `generateContent`, xUnit/NSubstitute, Vue 3/TypeScript, Python/PyMongo for the operator migration.

**Spec:** `docs/superpowers/specs/2026-09-30-email-cleanup-preferences-design.md`

## Global Constraints

- Cleanup `Trash` calls Gmail Trash, not permanent deletion; no automatic cleanup path calls Archive.
- New mail types enter `Review`; AI self-reported confidence alone never authorizes Trash.
- Only verified Vercel failed-deployment messages can bypass generic technical-alert protection. Financial sender protection remains absolute for automatic cleanup.
- User decisions require a written reason; stored examples contain ID, sender, subject, short snippet, decision, reason, optional tags, and timestamps, never full body.
- Proposed regex rules stay disabled until separately approved after server-side validation and preview.
- Existing 22 deletion feedback records read as `Trash`; retain historical archived action logs and unrelated manual archive behavior.
- Do not use a separate worktree. Work on `feat/53-email-cleanup-preferences` and follow `docs/agents/pr-merge-workflow.md` before a commit, push, PR, or merge.

## Review Focus

- Sender display name contains `vercel.com` but the actual address is unrelated: must not receive the Vercel exception (Task 1 test).
- A pending review and an active regex both match one message: no Gmail action and no second review (Task 4 test).
- Gemini cites a real `Trash` preference but changes the project/date in the subject: route to Review instead of automatic Trash (Task 3 test).
- Two simultaneous review resolutions or a retry after Gmail success: at most one Gmail Trash and one completed preference (Task 2 test).
- A proposed regex matches an old `Keep` example or protected sender: activation fails even when the UI preview was stale (Task 5 test).

---

## File map

- `GOpsHub.Domain/Enums/Enums.cs` and `GOpsHub.Domain/Entities/CleanupEntities.cs`: decision/status enums, `CleanupFeedback.Decision`, `CleanupReview`, rule approval metadata.
- `GOpsHub.Application/Features/EmailOps/EmailSafetyRules.cs`: verified sender parsing, Vercel exception, hard gates, regex validation.
- `GOpsHub.Application/Features/EmailOps/CleanupPreferenceSelector.cs` (new): relevant-example selection and exact known-type gate.
- `GOpsHub.Application/Features/EmailOps/Commands/CleanupReviewCommands.cs` (new): idempotent review resolution and preference creation.
- `GOpsHub.Application/Features/EmailOps/Queries/CleanupReviewQueries.cs` (new): pending review list.
- `GOpsHub.Application/Common/Interfaces/ICleanupReviewStore.cs` (new) and `GOpsHub.Infrastructure/Persistence/Repositories/MongoCleanupReviewStore.cs` (new): atomic review claim/complete/recovery beyond the generic repository.
- `GOpsHub.Application/Features/EmailOps/Queries/CleanupRulePreviewQuery.cs` (new): bounded rule preview.
- `GOpsHub.Application/Features/EmailOps/Commands/CleanupRuleCrudCommands.cs`: guarded rule activation and preview support.
- `GOpsHub.Application/Features/EmailOps/EmailCleanupBackgroundJob.cs`: orchestration only; delegate policy to safety/selector/review components.
- `GOpsHub.Application/Common/Interfaces/IServices.cs` and `GOpsHub.Infrastructure/AI/GeminiAIService.cs`: per-email AI response and prompt.
- `GOpsHub.Infrastructure/Persistence/MongoDbContext.cs`: `cleanup_reviews` mapping and unique message ID index setup.
- `GOpsHub.API/Controllers/EmailOpsController.cs`: review and rule-preview endpoints; migrate feedback and retire cleanup Archive actions.
- `src/frontend/src/components/email/{EmailActionLogList,CleanupRuleList,TeachAiCleanupModal}.vue` and `src/frontend/src/views/EmailOpsView.vue`: review UI, rule preview, honest copy.
- `docs/qa/email-cleanup-review-checklist.md` (new): manual interaction verification because this frontend has no component-test script.
- `tools/audit_cleanup_rules.py` (new): read-only default and guarded apply migration with a redacted backup/report.

### Task 1: Preference model and shared safety policy

**Files:** Modify `src/backend/GOpsHub.Domain/Enums/Enums.cs`, `src/backend/GOpsHub.Domain/Entities/CleanupEntities.cs`, `src/backend/GOpsHub.Application/Features/EmailOps/EmailSafetyRules.cs`, `src/backend/GOpsHub.Application/Features/EmailOps/Commands/CleanupFeedbackCommands.cs`, `src/backend/GOpsHub.Infrastructure/Persistence/MongoDbContext.cs`; test `src/backend/GOpsHub.Tests/Unit/EmailSafetyRulesTests.cs`, `src/backend/GOpsHub.Tests/Unit/CleanupFeedbackCommandHandlerTests.cs`.

**Interfaces:** Produce `CleanupDecision { Trash = 0, Keep = 1 }`, `CleanupReviewStatus { Pending, ProcessingTrash, Trashed, Kept }`, `CleanupRuleApprovalStatus { Draft, Approved, Rejected }`; `CleanupFeedback.Decision` with missing BSON value interpreted as `Trash` and optional `ReviewId` for idempotency; `CleanupRule.ApprovalStatus` and optional `SourceFeedbackId`; `CleanupReview { EmailId, Status, Sender, Subject, Snippet, AiReason, ProposedRuleId, ResolvedReason }`; and `EmailSafetyRules.IsSafeForAutomaticTrash(EmailMessage email, IEnumerable<string> whitelistDomains) : bool`.

- [ ] **Step 1:** Add failing tests `LegacyFeedbackDefaultsToTrash`, `VerifiedVercelFailureMayPassTechnicalAlertGuard`, `SpoofedVercelDisplayNameIsBlocked`, `FinancialSenderIsNeverAutoTrashed`, and `FeedbackRequiresWrittenReason`; assert the named outcomes and that no full body is persisted.
- [ ] **Step 2:** Run `dotnet test src/backend/GOpsHub.Tests --filter "FullyQualifiedName~EmailSafetyRulesTests|FullyQualifiedName~CleanupFeedbackCommandHandlerTests"`; confirm the new tests fail for the expected behavior.
- [ ] **Step 3:** Add model defaults, parse mailbox addresses, implement the safety gate, validate reason and short snippet length, and map `CleanupReview` to `cleanup_reviews`. Keep existing feedback documents readable without a bulk rewrite.
- [ ] **Step 4:** Re-run the filtered tests; expect PASS. Commit `feat: add cleanup preferences and automatic trash safety gates`.

### Task 2: Durable review workflow

**Files:** Create `src/backend/GOpsHub.Application/Features/EmailOps/Commands/CleanupReviewCommands.cs`, `src/backend/GOpsHub.Application/Features/EmailOps/Queries/CleanupReviewQueries.cs`, `src/backend/GOpsHub.Application/Common/Interfaces/ICleanupReviewStore.cs`, `src/backend/GOpsHub.Infrastructure/Persistence/Repositories/MongoCleanupReviewStore.cs`; modify `src/backend/GOpsHub.API/Controllers/EmailOpsController.cs`, `src/backend/GOpsHub.Infrastructure/Persistence/MongoDbContext.cs`, `src/backend/GOpsHub.Infrastructure/DependencyInjection.cs`; create `src/backend/GOpsHub.Tests/Unit/CleanupReviewCommandHandlerTests.cs`.

**Interfaces:** Produce `ResolveCleanupReviewCommand(string ReviewId, CleanupDecision Decision, string Reason) : ICommand<CleanupReview>` and `GetPendingCleanupReviewsQuery(int Page, int PageSize)`. `ICleanupReviewStore` exposes `TryClaimTrashAsync(string reviewId, CancellationToken ct) : Task<bool>`, `TryKeepAsync(string reviewId, string reason, CancellationToken ct) : Task<bool>`, `CompleteAsync(string reviewId, CleanupReviewStatus status, string reason, CancellationToken ct) : Task<CleanupReview>`, and `GetByIdAsync(string reviewId, CancellationToken ct) : Task<CleanupReview?>`; each state change uses Mongo compare-and-set. API: `GET /emailops/cleanup/reviews?status=Pending`; `POST /emailops/cleanup/reviews/{id}/resolve` with `{ decision, reason }`. Unique index on review `emailId` and sparse unique index on feedback `reviewId`.

- [ ] **Step 1:** Add failing tests for required reason, Trash writes Gmail action plus one preference, Keep writes no Gmail action plus one preference, repeated resolution, concurrent resolution, and Gmail-success/DB-failure retry reconciliation. Assert no duplicate Trash or completed preference.
- [ ] **Step 2:** Run `dotnet test src/backend/GOpsHub.Tests --filter FullyQualifiedName~CleanupReviewCommandHandlerTests`; confirm red.
- [ ] **Step 3:** Implement review query/command and endpoint; claim `Pending → ProcessingTrash` atomically before Gmail Trash, or `Pending → Kept` atomically for Keep. On retry, inspect `GetEmailByIdAsync(...).Labels` for `TRASH` before any new Gmail call, then complete the review and preference once using `ReviewId` as the idempotency key. If Gmail status cannot be read, leave the claim unresolved for operator reconciliation instead of sending a second Trash call. Add Mongo uniqueness/index creation; Task 7 imports any pre-existing `PendingApproval` logs.
- [ ] **Step 4:** Re-run the focused tests; expect PASS. Commit `feat: persist and resolve cleanup reviews safely`.

### Task 3: Stateless AI decision contract and preference selection

**Files:** Create `src/backend/GOpsHub.Application/Features/EmailOps/CleanupPreferenceSelector.cs`; modify `src/backend/GOpsHub.Application/Common/Interfaces/IServices.cs`, `src/backend/GOpsHub.Infrastructure/AI/GeminiAIService.cs`; test `src/backend/GOpsHub.Tests/Unit/GeminiAIServiceTests.cs`; create `src/backend/GOpsHub.Tests/Unit/CleanupPreferenceSelectorTests.cs`.

**Interfaces:** Produce `IAIService.AnalyzeCleanupBatchAsync(IReadOnlyList<EmailMessage> emails, IReadOnlyList<CleanupFeedback> examples, CancellationToken ct) : Task<IReadOnlyList<AICleanupDecision>>`, where `AICleanupDecision` contains `EmailId`, `Outcome` (`Trash`, `Review`, `Keep`), `Reason`, cited `FeedbackIds`, and optional `AIRegexRuleSuggestion`. Produce `CleanupPreferenceSelector.SelectRelevant(EmailMessage email, IReadOnlyList<CleanupFeedback> all, int limit) : IReadOnlyList<CleanupFeedback>` and `CanAutoTrashKnownType(EmailMessage email, AICleanupDecision decision, IReadOnlyList<CleanupFeedback> cited, IReadOnlyList<CleanupFeedback> relevantKeep) : bool`; known type requires same normalized sender address and subject plus a cited `Trash` feedback and no conflicting `Keep` feedback.

- [ ] **Step 1:** Add failing tests that the prompt includes relevant old Vercel feedback and Keep examples, uses no training/Archive claims, rejects response IDs absent from the batch, routes subject changes to Review, and permits only exact normalized known types through the automatic gate.
- [ ] **Step 2:** Run `dotnet test src/backend/GOpsHub.Tests --filter "FullyQualifiedName~GeminiAIServiceTests|FullyQualifiedName~CleanupPreferenceSelectorTests"`; confirm red.
- [ ] **Step 3:** Implement bounded relevant-example selection, per-message JSON parsing, defensive ID/enum validation, and the known-type gate. Preserve unrelated `IAIService` methods.
- [ ] **Step 4:** Re-run focused tests; expect PASS. Commit `feat: classify cleanup mail with persisted preferences`.

### Task 4: Cleanup job precedence and no-archive behavior

**Files:** Modify `src/backend/GOpsHub.Application/Features/EmailOps/EmailCleanupBackgroundJob.cs`, `src/backend/GOpsHub.Application/Features/EmailOps/Commands/CleanupCommands.cs`, `src/backend/GOpsHub.Tests/Unit/EmailCleanupBackgroundJobTests.cs`, `src/backend/GOpsHub.Tests/Unit/RunCleanupCommandHandlerTests.cs`.

**Interfaces:** Consume Tasks 1–3. `RunAutoCleanupAsync(CancellationToken)` retains the existing result shape for callers; review creation is idempotent by Gmail message ID. Only `CleanupRuleApprovalStatus.Approved` active rules execute. `EnsureDefaultRulesAsync` must not restore disabled or retired unsafe defaults.

- [ ] **Step 1:** Add failing tests for pending/Keep ahead of regex, protected sender ahead of AI, safe approved regex Trash, new AI type to Review, known exact type to Trash, AI failure to no action, no `ArchiveEmailAsync` call, and Vercel-only failure exception. Include a pending message that matches an active regex.
- [ ] **Step 2:** Run `dotnet test src/backend/GOpsHub.Tests --filter "FullyQualifiedName~EmailCleanupBackgroundJobTests|FullyQualifiedName~RunCleanupCommandHandlerTests"`; confirm red.
- [ ] **Step 3:** Refactor the job into ordered candidate filtering, regex handling, AI classification, review creation, and accurate metrics/logs. Remove cleanup Archive actions and unsafe default-rule seeding; preserve urgent notifications without allowing them to override a verified Vercel preference.
- [ ] **Step 4:** Re-run focused tests; expect PASS. Commit `feat: enforce safe trash review keep cleanup order`.

### Task 5: Regex draft validation and approval

**Files:** Create `src/backend/GOpsHub.Application/Features/EmailOps/Queries/CleanupRulePreviewQuery.cs`; modify `src/backend/GOpsHub.Application/Features/EmailOps/Commands/CleanupRuleCrudCommands.cs`, `src/backend/GOpsHub.Application/Features/EmailOps/Commands/CleanupCommands.cs`, `src/backend/GOpsHub.Application/Features/EmailOps/EmailSafetyRules.cs`, `src/backend/GOpsHub.API/Controllers/EmailOpsController.cs`, `src/backend/GOpsHub.Infrastructure/Alerting/TelegramBotPollingService.cs`; create `src/backend/GOpsHub.Tests/Unit/CleanupRuleApprovalTests.cs`.

**Interfaces:** Produce `PreviewCleanupRuleQuery(string RuleId) : IQuery<CleanupRulePreview>` and `ApproveCleanupRuleCommand(string RuleId) : ICommand<CleanupRule>`. `CleanupRulePreview` contains bounded sample matches and blockers, without full bodies. Telegram enable and existing toggle endpoints must call the same approval guard.

- [ ] **Step 1:** Add failing tests for match-any sender, Keep/pending/protected matches, invalid regex, duplicate active-or-disabled AND criteria, stale preview, and valid Vercel rule activation. Assert all unsafe paths leave `IsActive=false`.
- [ ] **Step 2:** Run `dotnet test src/backend/GOpsHub.Tests --filter FullyQualifiedName~CleanupRuleApprovalTests`; confirm red.
- [ ] **Step 3:** Implement draft proposal deduplication, preview, and a single activation guard used by API and Telegram. New manual rules start as Draft and inactive; only audited legacy rules become Approved in Task 7. Prevent automatic recreation of a rejected draft.
- [ ] **Step 4:** Re-run focused tests; expect PASS. Commit `feat: review regex rules before activation`.

### Task 6: Cleanup UI and honest terminology

**Files:** Modify `src/frontend/src/components/email/EmailActionLogList.vue`, `src/frontend/src/components/email/CleanupRuleList.vue`, `src/frontend/src/components/email/TeachAiCleanupModal.vue`, `src/frontend/src/views/EmailOpsView.vue`; create `docs/qa/email-cleanup-review-checklist.md`.

**Interfaces:** Consume Task 2 review endpoints and Task 5 preview/approve endpoints. Present Trash and Keep with required reason; remove cleanup Archive controls and preselected promotional tag; show source example and blocker/match preview for regex. Rename copy to “Ghi nhớ lý do dọn dẹp”.

- [ ] **Step 1:** Write a reproducible manual checklist covering required reason, Trash, Keep, pending count, blocked regex, and no Archive button; this frontend currently has no component-test script.
- [ ] **Step 2:** Run `npm run build` in `src/frontend`; record the baseline/result.
- [ ] **Step 3:** Replace pending-action controls with review controls and wire the new endpoints. Update rule review and user-facing copy, keeping historical log display intact.
- [ ] **Step 4:** Re-run frontend build and interaction checklist; expect no type/build errors and the specified UI states. Commit `feat: show cleanup reviews and rule safety previews`.

### Task 7: Audit and migrate live cleanup rules

**Files:** Create `tools/audit_cleanup_rules.py`, `tools/test_audit_cleanup_rules.py`; modify `CONTEXT.md`, `docs/adr/0001-user-cleanup-feedback-few-shot.md`.

**Interfaces:** The tool reads `MONGODB_CONNECTION_STRING` and `MONGODB_DATABASE_NAME`; default is `--dry-run`. `--apply` performs idempotent rule updates only after writing a local backup/report outside the repo or in an ignored local directory. It imports any legacy `PendingApproval` logs into `cleanup_reviews`. Surviving legacy active rules receive `approvalStatus=Approved`; unsafe ones become inactive Draft. It must never move Gmail messages or print credentials/body text.

- [ ] **Step 1:** Add fixture tests for 42-rule inventory logic, both match-any sender rules, broad Google/GitHub/account alerts, Vercel narrowing, duplicate handling, idempotent second run, and redacted output.
- [ ] **Step 2:** Run `python -m unittest tools/test_audit_cleanup_rules.py`; confirm red.
- [ ] **Step 3:** Implement the tool and update domain/ADR wording. Produce a per-rule dry-run report with before/after IDs, action, regex, and reason; keep disabled records for rollback.
- [ ] **Step 4:** Re-run `python -m unittest tools/test_audit_cleanup_rules.py` and run the live DB `--dry-run`; inspect every proposed update and correct unexpected changes. Commit `chore: audit cleanup rules and document preference semantics`.

### Task 8: Final verification and PR

**Files:** Update tests or docs only for defects found by the required checks.

**Interfaces:** The completed branch addresses GitHub issue #53 and references this plan and spec.

- [ ] **Step 1:** Run `dotnet test` and `dotnet build` from `src/backend`, then `npm run build` from `src/frontend`; fix only concrete failures and re-run the affected check.
- [ ] **Step 2:** Run the reviewed migration `--apply` against the configured DB, re-run `--dry-run` to verify zero further changes, and capture aggregate counts without private message content.
- [ ] **Step 3:** Review `git diff origin/main`, `git status`, `.gitignore`, and staged changes for keys, tokens, connection strings, or private email contents. Confirm no `.env`, `*.pem`, or `*.key` is tracked.
- [ ] **Step 4:** Commit any verification fixes, push `feat/53-email-cleanup-preferences`, and create a PR linked to #53 with the test results and DB migration outcome.
- [ ] **Step 5:** Review the PR checks and diff. Merge into `main` according to `docs/agents/pr-merge-workflow.md` once all required checks pass; update local `main`.
