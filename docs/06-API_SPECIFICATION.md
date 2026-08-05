# 🌐 G-Ops Hub — API Specification

> RESTful API specification cho hệ thống G-Ops Hub.
> Base URL: `/api/v1`

---

## 1. Authentication

### `POST /api/v1/auth/google-login`
Đăng nhập bằng Google OAuth.

**Request Body**:
```json
{ "idToken": "google-id-token-from-frontend" }
```

**Response** `200 OK`:
```json
{
  "accessToken": "jwt-token",
  "expiresIn": 900,
  "user": {
    "id": "guid",
    "email": "user@gmail.com",
    "displayName": "User Name",
    "avatarUrl": "https://...",
    "role": "member"
  }
}
```

### `POST /api/v1/auth/refresh`
Refresh JWT token (sử dụng httpOnly cookie).

### `POST /api/v1/auth/logout`
Logout, invalidate refresh token.

### `GET /api/v1/auth/me`
Lấy thông tin user hiện tại.

---

## 2. Dashboard

### `GET /api/v1/dashboard/summary`
Lấy tổng hợp Dashboard.

**Response**: Tổng email cleaned, pending drafts, transactions today, active alerts, system health.

### `GET /api/v1/dashboard/activity-feed?page=1&pageSize=20`
Lấy activity feed (mọi hoạt động gần đây).

---

## 3. Email Operations

### Cleanup Rules

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| `GET` | `/api/v1/email/cleanup-rules` | Danh sách rules |
| `POST` | `/api/v1/email/cleanup-rules` | Tạo rule mới |
| `PUT` | `/api/v1/email/cleanup-rules/{id}` | Cập nhật rule |
| `DELETE` | `/api/v1/email/cleanup-rules/{id}` | Xóa rule |
| `POST` | `/api/v1/email/cleanup/run` | Chạy cleanup manual |
| `GET` | `/api/v1/email/cleanup/logs?page=1&pageSize=20` | Lịch sử cleanup |

### AI Drafts

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| `GET` | `/api/v1/email/drafts?status=pending&page=1` | Danh sách drafts |
| `GET` | `/api/v1/email/drafts/{id}` | Chi tiết draft |
| `POST` | `/api/v1/email/drafts/{id}/approve` | Duyệt & gửi |
| `POST` | `/api/v1/email/drafts/{id}/reject` | Từ chối draft |
| `PUT` | `/api/v1/email/drafts/{id}/edit` | Chỉnh sửa & gửi |

---

## 4. Scheduling & Calendar

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| `GET` | `/api/v1/public/calendar-status` | (Public Anonymous) Lấy danh sách lịch bận/sự kiện theo khoảng ngày (`startDate`, `endDate`) |
| `GET` | `/api/v1/scheduling/upcoming` | (Admin) Lấy danh sách lịch sắp tới (`startDate`, `endDate`) |
| `POST` | `/api/v1/scheduling/event` | (Admin) Tạo mới lịch hẹn (`isPublic` true/false, sync Google Calendar) |
| `POST` | `/api/v1/tasks/task` | (Admin) Tạo mới công việc (tùy chọn sync Google Calendar + `isPublic`) |

---

## 5. Finance (UC04)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| `GET` | `/api/v1/finance/transactions` | Lấy danh sách giao dịch phân trang (`page`, `pageSize`) |
| `GET` | `/api/v1/finance/transactions/pending` | Lấy danh sách email ngân hàng chưa đọc chờ xử lý |
| `POST` | `/api/v1/finance/transactions/parse` | Parse email giao dịch đơn lẻ qua AI & sync Google Sheets |
| `POST` | `/api/v1/finance/transactions/sync-batch` | Nén & parse hàng loạt email ngân hàng qua AI & sync Google Sheets |
| `GET` | `/api/v1/finance/config` | Lấy cấu hình xuất Google Drive & Sheets (Folder ID, FileName Pattern, Spreadsheet ID) |
| `POST` | `/api/v1/finance/config` | Lưu cấu hình xuất Google Drive & Sheets |

---

## 6. Drive Guard

### Monitored Folders

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| `GET` | `/api/v1/drive/folders` | Danh sách folder đang theo dõi |
| `POST` | `/api/v1/drive/folders` | Thêm folder theo dõi |
| `PUT` | `/api/v1/drive/folders/{id}` | Cập nhật cấu hình |
| `DELETE` | `/api/v1/drive/folders/{id}` | Dỡ theo dõi |

### Audit Logs & Alerts

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| `GET` | `/api/v1/drive/audit-logs?folderId=...&page=1` | Lịch sử tác động |
| `GET` | `/api/v1/drive/alerts?severity=critical&page=1` | Danh sách cảnh báo |
| `POST` | `/api/v1/drive/alerts/{id}/resolve` | Đánh dấu đã xử lý |
| `POST` | `/api/v1/drive/alerts/{id}/quarantine` | Quarantine file |

### Permission Monitor

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| `GET` | `/api/v1/drive/permissions/report` | Báo cáo quyền truy cập |
| `POST` | `/api/v1/drive/permissions/{fileId}/revoke` | Thu hồi quyền public |

---

## 7. Backup

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| `GET` | `/api/v1/backups?page=1` | Lịch sử backup |
| `POST` | `/api/v1/backups/trigger` | Trigger backup thủ công |
| `GET` | `/api/v1/backups/{id}` | Chi tiết backup |

---

## 8. Notifications

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| `GET` | `/api/v1/notifications?unreadOnly=true&page=1` | Danh sách thông báo |
| `POST` | `/api/v1/notifications/{id}/read` | Đánh dấu đã đọc |
| `POST` | `/api/v1/notifications/read-all` | Đọc tất cả |

---

## 9. Admin (Require Admin/SuperAdmin role)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| `GET` | `/api/v1/admin/users?page=1` | Danh sách users |
| `PUT` | `/api/v1/admin/users/{id}/role` | Đổi role user |
| `PUT` | `/api/v1/admin/users/{id}/status` | Enable/Disable user |
| `GET` | `/api/v1/admin/system/health` | System health check |
| `GET` | `/api/v1/admin/system/settings` | System settings |
| `PUT` | `/api/v1/admin/system/settings` | Cập nhật settings |
| `GET` | `/api/v1/admin/jobs` | Background jobs status |

---

## 10. Webhooks (Internal)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| `POST` | `/api/v1/webhooks/drive` | Google Drive push notification |
| `POST` | `/api/v1/webhooks/gmail` | Gmail push notification (future) |

---

## 11. Standard Response Format

### Success Response
```json
{
  "success": true,
  "data": { ... },
  "message": null
}
```

### Paginated Response
```json
{
  "success": true,
  "data": {
    "items": [ ... ],
    "totalCount": 150,
    "page": 1,
    "pageSize": 20,
    "totalPages": 8
  }
}
```

### Error Response
```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": {
    "ruleName": ["Rule name is required"],
    "olderThanDays": ["Must be between 1 and 365"]
  }
}
```

---

## 12. SignalR Hub

**Hub URL**: `/hubs/notifications`

### Server → Client Events

| Event | Payload | Trigger |
|-------|---------|---------|
| `ReceiveNotification` | `{ title, message, severity, category }` | Mọi notification mới |
| `CleanupCompleted` | `{ totalProcessed, totalTrashed }` | Cleanup job hoàn thành |
| `NewDraftReady` | `{ draftId, subject, confidence }` | AI tạo xong draft |
| `ScheduleExtracted` | `{ scheduleId, title, startTime }` | Schedule mới extracted |
| `TransactionLogged` | `{ amount, type, bank }` | Giao dịch mới ghi nhận |
| `SecurityAlert` | `{ alertId, severity, fileName }` | Cảnh báo bảo mật |
| `DriveActivity` | `{ fileId, action, actor }` | Thay đổi trên Drive |
