# 💾 G-Ops Hub — Database Design (MongoDB)

> Thiết kế cơ sở dữ liệu MongoDB Atlas (Free Tier M0) cho hệ thống G-Ops Hub.

---

## 1. Database Info

| Property | Value |
|----------|-------|
| Provider | MongoDB Atlas |
| Tier | M0 (Free — 512MB) |
| Driver | MongoDB.Driver (.NET) |
| Database Name | `gopshub` |

---

## 2. Collections Overview

| Collection | Mô tả | Estimated Doc Size |
|------------|--------|-------------------|
| `admin_user` | Admin user duy nhất + settings | ~2KB (1 doc) |
| `cleanup_rules` | Quy tắc dọn email | ~0.5KB × N rules |
| `cleanup_logs` | Lịch sử chạy cleanup | ~0.3KB × N logs |
| `ai_drafts` | AI-generated email drafts | ~3KB × N drafts |
| `reply_templates` | Email reply templates | ~1KB × N templates |
| `extracted_schedules` | Lịch hẹn extracted từ email | ~1KB × N schedules |
| `transactions` | Giao dịch tài chính | ~0.5KB × N transactions |
| `monitored_folders` | Thư mục Drive đang theo dõi | ~0.5KB × N folders |
| `drive_audit_logs` | Lịch sử thay đổi Drive | ~0.4KB × N logs |
| `security_alerts` | Cảnh báo bảo mật | ~0.5KB × N alerts |
| `notifications` | Thông báo cho dashboard | ~0.3KB × N notifs |
| `backup_records` | Lịch sử backup | ~0.3KB × N records |

> Với 512MB free tier, ước tính đủ cho ~500K documents. Dư sức cho dự án cá nhân.

---

## 3. Document Schemas

### 3.1 admin_user (Singleton — chỉ 1 document)

```json
{
  "_id": "ObjectId",
  "email": "hnt.vn.vn@gmail.com",
  "displayName": "string",
  "avatarUrl": "string",
  "googleId": "string",
  "googleAccessToken": "encrypted-string",
  "googleRefreshToken": "encrypted-string",
  "googleTokenExpiresAt": "ISODate",
  "settings": {
    "cleanupEnabled": true,
    "cleanupSchedule": "0 0 * * *",
    "aiDraftEnabled": true,
    "aiDraftLanguage": "vi",
    "scheduleExtractionEnabled": true,
    "transactionLoggingEnabled": true,
    "transactionSmallThreshold": 500000,
    "timezone": "Asia/Ho_Chi_Minh",
    "language": "vi",
    "notificationChannels": {
      "dashboard": true,
      "email": false,
      "discordWebhookUrl": "string | null"
    }
  },
  "lastLoginAt": "ISODate",
  "createdAt": "ISODate",
  "updatedAt": "ISODate"
}
```

---

### 3.2 cleanup_rules

```json
{
  "_id": "ObjectId",
  "ruleName": "Clean Promotions",
  "category": "promotions | social | updates | forums | custom",
  "olderThanDays": 7,
  "action": "trash | archive",
  "whitelistDomains": ["important.com", "keep.org"],
  "customQuery": "from:newsletters@example.com",
  "isActive": true,
  "createdAt": "ISODate",
  "updatedAt": "ISODate"
}
```

### 3.3 cleanup_logs

```json
{
  "_id": "ObjectId",
  "ruleId": "ObjectId (ref cleanup_rules)",
  "ruleName": "Clean Promotions",
  "executedAt": "ISODate",
  "totalProcessed": 47,
  "totalTrashed": 35,
  "totalArchived": 12,
  "totalSkipped": 0,
  "durationMs": 2340,
  "details": "Processed 47 emails from Promotions category"
}
```

---

### 3.4 ai_drafts

```json
{
  "_id": "ObjectId",
  "originalEmail": {
    "gmailMessageId": "string",
    "from": "professor@uni.edu",
    "subject": "RE: Đồ án cuối kỳ",
    "snippet": "Em cần nộp bài trước ngày...",
    "receivedAt": "ISODate"
  },
  "gmailDraftId": "string | null",
  "draftContent": "Chào Thầy, em xin phép xác nhận...",
  "confidenceScore": 0.87,
  "status": "pending | approved | rejected | edited | sent | expired",
  "userFeedback": "string | null",
  "editedContent": "string | null",
  "createdAt": "ISODate",
  "processedAt": "ISODate | null"
}
```

---

### 3.5 reply_templates

```json
{
  "_id": "ObjectId",
  "templateName": "Academic Reply",
  "category": "academic | work | personal | formal",
  "content": "Chào {{recipient}},\nEm/tôi xin phép...",
  "language": "vi | en",
  "usageCount": 5,
  "createdAt": "ISODate",
  "updatedAt": "ISODate"
}
```

---

### 3.6 extracted_schedules

```json
{
  "_id": "ObjectId",
  "sourceEmailId": "gmail-message-id",
  "sourceEmailSubject": "Interview Schedule - Google",
  "title": "Phỏng vấn Google - Round 2",
  "startTime": "ISODate",
  "endTime": "ISODate",
  "location": "Google Meet: https://meet.google.com/abc",
  "description": "Technical interview round 2",
  "eventType": "interview | flight | meeting | appointment | deadline",
  "calendarEventId": "google-calendar-event-id | null",
  "confidenceScore": 0.92,
  "status": "auto_created | pending_confirm | confirmed | rejected",
  "createdAt": "ISODate"
}
```

---

### 3.7 transactions

```json
{
  "_id": "ObjectId",
  "sourceEmailId": "gmail-message-id",
  "transactionDate": "ISODate",
  "bankName": "VCB | TCB | MBBank | VPBank | Momo | ZaloPay",
  "transactionType": "credit | debit",
  "amount": 5000000,
  "feeAmount": 0,
  "transactionCode": "FT26080512345",
  "sourceAccount": "1012345678",
  "targetAccount": "9087654321",
  "beneficiaryName": "NGUYEN VAN A",
  "currency": "VND",
  "description": "Luong thang 8/2026",
  "category": "salary | food | transport | bills | shopping | transfer | other",
  "balanceAfter": 12500000,
  "sheetRowRef": "BaoCaoTaiChinh_2026_08!A15",
  "isAutoRead": true,
  "createdAt": "ISODate"
}
```

### 3.8 app_configurations

```json
{
  "_id": "ObjectId",
  "key": "Finance_FolderId | Finance_FileNamePattern | Finance_SpreadsheetId | DriveGuardInterval",
  "value": "string",
  "createdAt": "ISODate",
  "updatedAt": "ISODate"
}
```

---

### 3.8 monitored_folders

```json
{
  "_id": "ObjectId",
  "googleFolderId": "drive-folder-id",
  "folderName": "Shared Project Alpha",
  "folderPath": "/Shared/Project-Alpha",
  "isActive": true,
  "alertOnBulkDelete": true,
  "bulkDeleteThreshold": 5,
  "watchChannelId": "drive-watch-channel-id | null",
  "watchExpiration": "ISODate | null",
  "createdAt": "ISODate",
  "updatedAt": "ISODate"
}
```

---

### 3.9 drive_audit_logs

```json
{
  "_id": "ObjectId",
  "monitoredFolderId": "ObjectId (ref monitored_folders)",
  "googleFileId": "drive-file-id",
  "fileName": "Budget_Q3.xlsx",
  "fileType": "application/vnd.ms-excel",
  "action": "created | modified | deleted | trashed | moved | shared | permission_changed | renamed",
  "actorEmail": "collaborator@gmail.com",
  "actorName": "Collaborator Name",
  "actionTimestamp": "ISODate",
  "details": "File renamed from Budget_Q2.xlsx to Budget_Q3.xlsx"
}
```

---

### 3.10 security_alerts

```json
{
  "_id": "ObjectId",
  "severity": "info | warning | high | critical",
  "alertType": "suspicious_file | bulk_delete | permission_drift | unauthorized_access",
  "fileId": "drive-file-id | null",
  "fileName": "virus_scanner.exe",
  "filePath": "/Shared/Project-Alpha/virus_scanner.exe",
  "description": "Dangerous file extension (.exe) detected",
  "isResolved": false,
  "resolvedAt": "ISODate | null",
  "resolutionNote": "string | null",
  "createdAt": "ISODate"
}
```

---

### 3.11 notifications

```json
{
  "_id": "ObjectId",
  "title": "AI Draft Ready",
  "message": "New draft for email from professor@uni.edu",
  "severity": "info | warning | critical",
  "category": "email | calendar | finance | drive | system",
  "actionUrl": "/email/drafts/abc123",
  "isRead": false,
  "readAt": "ISODate | null",
  "createdAt": "ISODate"
}
```

---

### 3.12 backup_records

```json
{
  "_id": "ObjectId",
  "backupType": "transaction_export | audit_log_export | full_snapshot",
  "googleFileId": "drive-file-id | null",
  "fileName": "backup_transactions_2026_08.csv",
  "fileSizeBytes": 45230,
  "driveFolder": "/G-Ops Backups/2026-08/",
  "status": "in_progress | completed | failed",
  "errorMessage": "string | null",
  "startedAt": "ISODate",
  "completedAt": "ISODate | null"
}
```

---

## 4. Indexes

| Collection | Index Fields | Type | Mục đích |
|------------|-------------|------|----------|
| `cleanup_logs` | `{ executedAt: -1 }` | Descending | Query logs gần nhất |
| `cleanup_logs` | `{ ruleId: 1, executedAt: -1 }` | Compound | Logs theo rule |
| `ai_drafts` | `{ status: 1, createdAt: -1 }` | Compound | Pending drafts |
| `ai_drafts` | `{ originalEmail.gmailMessageId: 1 }` | Unique-sparse | Dedup |
| `extracted_schedules` | `{ status: 1, startTime: 1 }` | Compound | Upcoming schedules |
| `transactions` | `{ transactionDate: -1 }` | Descending | Monthly reports |
| `transactions` | `{ sourceEmailId: 1 }` | Unique | Dedup |
| `drive_audit_logs` | `{ monitoredFolderId: 1, actionTimestamp: -1 }` | Compound | Audit timeline |
| `security_alerts` | `{ isResolved: 1, createdAt: -1 }` | Compound | Active alerts |
| `notifications` | `{ isRead: 1, createdAt: -1 }` | Compound | Unread feed |

---

## 5. Data Retention Policy

Để giữ storage trong giới hạn 512MB free tier:

| Collection | Retention | Cleanup Strategy |
|------------|-----------|-----------------|
| `cleanup_logs` | 90 ngày | TTL index hoặc cron job |
| `ai_drafts` | 180 ngày | TTL cho status=sent/rejected |
| `drive_audit_logs` | 90 ngày | TTL index |
| `notifications` | 30 ngày (read), 90 ngày (unread) | Cron job |
| `transactions` | Vĩnh viễn | Backup rồi archive yearly |
| `security_alerts` | 180 ngày (resolved) | TTL cho resolved alerts |

### MongoDB TTL Index Example

```javascript
// Auto-delete notifications older than 30 days that are read
db.notifications.createIndex(
  { "readAt": 1 },
  { expireAfterSeconds: 2592000 } // 30 days
);
```
