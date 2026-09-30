# Google Workspace Personal Assistant (GOpsHub)

An intelligent automation assistant integrating Google Workspace APIs, scheduled background processing, and Gemini AI to maintain inbox hygiene, extract calendar schedules, and audit financial telemetry.

## Language

**CleanupFeedback**:
A saved user decision to move an email to Trash or Keep it, with a written reason. The decision can guide later classifications of similar email.
_Avoid_: DeletionSample, FeedbackRecord, TrainingData

**CleanupRule**:
A reviewed sender-and-subject pattern that can move matching eligible unread email to Trash.
_Avoid_: Filter, MailFilter, DiscardPolicy

**CleanupReview**:
A pending user decision about an email the system proposed for cleanup. The user chooses Trash or Keep and records a reason.
_Avoid_: PendingApprovalLog, AITrainingSample

**EmailActionLog**:
An immutable audit record capturing actions performed on emails, the associated reason, and the initiating process.
_Avoid_: AuditEntry, MailHistory, ExecutionLog

**CleanupLog**:
A summarized batch execution record of emails considered and moved to Trash during a cleanup run.
_Avoid_: RunSummary, JobRecord

**UrgentActionEmail**:
A high-priority email requiring explicit user intervention, protected from automatic cleanup unless the user has approved a specific exception.
_Avoid_: CriticalMail, AlertEmail
