# Google Workspace Personal Assistant (GOpsHub)

An intelligent automation assistant integrating Google Workspace APIs, scheduled background processing, and Gemini AI to maintain inbox hygiene, extract calendar schedules, and audit financial telemetry.

## Language

**CleanupFeedback**:
A user-submitted retention preference indicating that a specific email should be deleted along with the user's rationale, used as few-shot guidance for AI cleanup.
_Avoid_: DeletionSample, FeedbackRecord, TrainingData

**CleanupRule**:
A pattern-matching rule specifying regex criteria and actions (trash or archive) for automated inbox cleaning.
_Avoid_: Filter, MailFilter, DiscardPolicy

**EmailActionLog**:
An immutable audit record capturing actions performed on emails, the associated reason, and the initiating process.
_Avoid_: AuditEntry, MailHistory, ExecutionLog

**CleanupLog**:
A summarized batch execution record aggregating metrics on emails processed, trashed, and archived during a cleanup run.
_Avoid_: RunSummary, JobRecord

**UrgentActionEmail**:
A high-priority email requiring explicit user intervention, protected from automated deletion.
_Avoid_: CriticalMail, AlertEmail
