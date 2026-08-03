namespace GOpsHub.Domain.Enums;

public enum CleanupAction
{
    Trash = 0,
    Archive = 1
}

public enum DraftStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Edited = 3,
    Sent = 4,
    Expired = 5
}

public enum ScheduleStatus
{
    AutoCreated = 0,
    PendingConfirm = 1,
    Confirmed = 2,
    Rejected = 3
}

public enum TransactionType
{
    Credit = 0,  // Thu (income)
    Debit = 1    // Chi (expense)
}

public enum DriveAction
{
    Created = 0,
    Modified = 1,
    Deleted = 2,
    Trashed = 3,
    Moved = 4,
    Shared = 5,
    PermissionChanged = 6,
    Renamed = 7
}

public enum AlertSeverity
{
    Info = 0,
    Warning = 1,
    High = 2,
    Critical = 3
}

public enum AlertType
{
    SuspiciousFile = 0,
    BulkDelete = 1,
    PermissionDrift = 2,
    UnauthorizedAccess = 3
}

public enum BackupType
{
    TransactionExport = 0,
    AuditLogExport = 1,
    FullSnapshot = 2
}

public enum BackupStatus
{
    InProgress = 0,
    Completed = 1,
    Failed = 2
}

public enum EventType
{
    Interview = 0,
    Flight = 1,
    Meeting = 2,
    Appointment = 3,
    Deadline = 4,
    Other = 5
}
