# Email cleanup with durable user preferences

Date: 2026-09-30
Status: Awaiting review of the written spec

## Purpose

Clean unread, unstarred Inbox mail according to the owner's explicit preferences while avoiding accidental deletion of important alerts. A cleanup decision means moving a message to Gmail Trash, never permanent deletion. Automated cleanup no longer archives mail. Manual archive features outside cleanup are out of scope.

The model API is stateless in this application. The product will describe saved examples as remembered cleanup preferences, not as model training or a persistent AI session.

## Current behavior and evidence

- The job selects up to 100 unread, unstarred Inbox messages, handles urgent subjects, runs active regex rules first, and sends up to 15 remaining messages to Gemini.
- A single Gemini response can auto trash or archive target IDs when its self-reported confidence is at least 0.85. Lower confidence creates `PendingApproval` action logs. Proposed regex rules are disabled.
- `CleanupFeedback` already stores a deleted email's sender, subject, snippet, tags, and reason. Only the 10 newest examples enter the prompt; the UI calls this training.
- A pending message is excluded from the AI stage but can still be processed by the earlier regex stage. Approving or dismissing a pending action does not create a preference.
- Read-only audit on 2026-09-30: 42 rules in MongoDB, 41 active, all 42 configured for Trash; 22 deletion feedback records and zero pending action logs. Two active rules have a sender regex matching every sender. One matches `Failed deployment` irrespective of origin. The default Vercel rule also spans `notifications@github.com`. Other active rules cover account sharing, sign-in, and platform failure notifications.

## Approved policy

1. Cleanup has three outcomes per message: `Trash`, `Review`, and `Keep`. `Review` means leave the message untouched in Inbox until the owner decides. `Keep` means leave it untouched and remember the owner's reason when explicitly selected.
2. A new mail type is reviewed before it can be automatically trashed. A user-approved deletion can make later messages of the same type eligible for automatic Trash. A new AI-generated regex never becomes active without separate approval.
3. Vercel failed-deployment notifications are the only pre-approved exception to generic technical-alert protection. The exception requires a verified Vercel sender and a failed-deployment subject; it does not extend to GitHub Actions, Google Apps Script, MongoDB Atlas, or other alerts.
4. Financial institution and wallet senders remain protected from automated Trash even when AI proposes it. A specific message may be trashed manually, but feedback from it must not create an automatic domain-wide financial rule.
5. Every manual `Trash` or `Keep` decision used as a preference requires an explicit reason. No tag is selected by default. Saved examples contain only Gmail ID, sender, subject, short snippet, decision, reason, optional tags, and timestamps; they do not store the full body.
6. No automatic cleanup path uses Archive. Historical `Archived` audit entries are retained as history.

## Architecture and data flow

### 1. Candidate selection and safety

Continue selecting unread, unstarred Inbox messages. Before any rule or model decision, exclude messages with an open cleanup review or an explicit `Keep` preference for that message. Apply sender and alert safety checks to both regex and AI paths. Parse the actual sender address/domain rather than treating a display-name substring as trusted. Restrict the Vercel exception to verified Vercel sender addresses and the failed-deployment class.

An active, user-approved `CleanupRule` may Trash a safe matching message. All configured sender, subject, and body criteria retain AND semantics and a regex timeout. An exact-message `Keep` preference or open review wins over a matching regex. If a rule has invalid syntax, times out, or fails the safety gate, it does not act on the message. At rule activation time, the same rule criteria are tested against stored `Keep` examples to prevent broader conflicts.

### 2. AI decision

Send safe, unmatched candidates in bounded batches with relevant `Trash` and `Keep` examples and their reasons. Select examples by sender address/domain and subject similarity first, then recent diverse examples as remaining budget allows. Do not rely exclusively on the newest ten. The prompt must describe the owner's policy, the narrow Vercel exception, and the meaning of each outcome; it must not present newsletters as an Archive case.

The AI returns a per-message outcome, rationale, cited preference IDs when claiming a known type, and optional regex proposal. The server accepts only IDs from the candidate batch. Self-reported confidence is advisory. A server-side gate permits AI-initiated Trash only when the message is safe, the AI cites an existing approved `Trash` preference, no relevant `Keep` preference conflicts, and the candidate has the same normalized sender address and normalized subject as the cited example. Normalization is case folding plus whitespace collapse; it does not remove project names, dates, or other subject content. Broader reusable matches require an approved regex. All other AI suggestions to delete become `Review`; an AI `Keep` leaves the message untouched without creating a durable user preference. Malformed responses or API failures leave messages untouched and record a diagnostic error.

### 3. Reviews and preferences

Introduce a `CleanupReview` record with a unique Gmail message ID and explicit `Pending`, `Trashed`, or `Kept` state. It stores a short candidate snapshot, AI rationale, and proposed regex reference if present. The job never reprocesses a `Pending` review. Resolution is idempotent and checks the current state; concurrent or repeated decisions cannot trigger a second Gmail action.

The pending-review UI offers only `Trash` and `Keep`, each requiring a user-written reason. Trash moves the Gmail message to Trash and saves a `Trash` preference; Keep leaves Gmail untouched and saves a `Keep` preference. Existing deletion feedback becomes `Trash` by default without losing the 22 records. User-initiated `Trash & remember` on an Inbox email uses the same preference model. A separate immutable `EmailActionLog` is written for completed actions; review state is not represented by editing an action log. Existing pending logs, if any appear during migration, are converted or surfaced through the new review endpoint before the old action-log approval endpoints are retired.

### 4. Regex proposals and approval

AI may propose a `CleanupRule` in disabled state after a reviewed deletion. Deduplicate proposals against active and disabled rules by their full AND criteria, not by either regex alone. Require syntax validation, timeout protection, and a narrow sender condition. A rule that matches a stored `Keep` example, a pending review, a protected sender, or an unrelated technical alert cannot be enabled unchanged.

The rule review UI shows its sender/subject/body criteria, the deletion reason and source example, and a dry-run preview against available recent messages and stored `Keep` examples. Approval rechecks these guards on the server, then activates the rule. A single message can provide a proposed rule, but never bypasses this approval. A disabled or rejected rule is not recreated automatically just because the next job sees the same sample.

### 5. Existing-rule audit and migration

Ship a repeatable, idempotent audit/migration with a dry-run report before DB writes. Inventory all 42 rules, validate regex syntax, classify unbounded sender patterns and security/account/technical-alert patterns, find overlaps, and evaluate representative matches against available historical action logs and saved `Keep` examples. Disable broad or unsafe rules, including the two match-any-sender rules; narrow the Vercel rule to verified Vercel senders and failed-deployment subjects; keep narrow rules that pass checks. Preserve rule records and their prior values in the migration report for review and rollback. Do not re-create disabled defaults in `EnsureDefaultRulesAsync`.

The migration changes live MongoDB rules only after code and tests are ready and the dry-run output has been reviewed. Gmail messages are not moved by the migration.

## Failure handling and consistency

- AI errors, malformed IDs, absent cited preferences, safety conflicts, and regex errors fail closed: no automatic Trash.
- Resolve review actions in a recoverable order. If Gmail succeeds and persistence fails, retry/reconcile from the known message/review ID without blindly repeating the action or reporting complete success. If Gmail fails, leave the review pending and save no completed `Trash` preference.
- Enforce uniqueness of active review per Gmail ID and uniqueness or idempotency of explicit preference submissions. Keep the audit trail of what actually happened, including failures and retries, without exposing message bodies or credentials.
- Rule creation and activation validate against current DB state so stale previews cannot turn on an unsafe rule.

## Interfaces and copy

- Replace cleanup pending-action APIs with review APIs that accept decision and required reason. Keep compatibility for existing pending records during migration.
- Update the cleanup feedback API/model to represent `Trash` and `Keep`, while preserving old records as `Trash`.
- Remove Archive controls from cleanup review and cleanup rule editing. Retain historical archived entries in logs.
- Replace “Huấn luyện AI” wording with “Ghi nhớ lý do dọn dẹp” or equivalent language explaining that saved examples are supplied on later AI calls.
- Update `CONTEXT.md` and ADR-0001 to describe preferences and the separately approved regex lifecycle. The current ADR's warning against brittle one-email regex remains valid: a single example can propose a draft, not activate it.

## Verification and acceptance

1. Unit and integration tests cover the full precedence order: pending/Keep, protected senders/alerts, approved regex, then AI. Pending and Keep messages never reach Trash through regex or AI.
2. Verify Vercel failed deployment from a verified Vercel sender can follow the approved preference; the same subject from another sender and other technical failures cannot auto Trash.
3. Verify no cleanup path calls Archive, new types enter review, explicit reasons persist, relevant old preferences reach later prompts, and Keep examples block conflicting proposals.
4. Verify invalid or overbroad regex cannot be activated, duplicate proposals are suppressed, and a failed AI/Gmail/DB operation does not claim full success.
5. Run backend tests and build, frontend build, a dry-run rule audit, then review the resulting DB changes. Follow the repo's secrets hygiene checks before committing or opening a PR.
