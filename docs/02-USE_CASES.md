# 📋 G-Ops Hub — Use Cases Specification

> Chi tiết đặc tả từng Use Case trong hệ thống G-Ops Hub.

---

## Requirements Matrix

| Module | UC ID | Tên | Google APIs | Priority |
|--------|-------|-----|-------------|----------|
| Email Ops | UC01 | Auto-Clean Inbox | Gmail API | 🔴 Must-Have |
| Email Ops | UC02 | Human-in-the-Loop AI Draft | Gmail API | 🔴 Must-Have |
| Email Ops | UC07 | Auto Attachment Quarantine | Gmail + Drive API | 🟡 Should-Have |
| Email Ops | UC08 | Executive Thread Summarizer | Gmail + Docs API | 🟢 Nice-to-Have |
| Scheduling | UC03 | AI Schedule Extractor | Gmail + Calendar API | 🔴 Must-Have |
| Finance | UC04 | Transaction Telemetry | Gmail + Sheets API | 🔴 Must-Have |
| Drive Guard | UC05 | Real-time Folder Audit | Drive API | 🔴 Must-Have |
| Drive Guard | UC06 | Suspicious File Guard | Drive API | 🔴 Must-Have |
| Drive Guard | UC09 | Permission Drift Monitor | Drive API | 🟡 Should-Have |
| Backup | UC10 | Snapshot & Backup Engine | Sheets + Drive API | 🟡 Should-Have |
| Workflow | UC11 | Form Response Processing | Forms + Sheets + Calendar | 🟢 Nice-to-Have |
| Alerting | UC12 | Multi-Channel Alerting | Chat / Discord Webhook | 🟡 Should-Have |

---

## Module 1: Email Ops (Gmail Engine)

### UC01 — Auto-Clean & Inbox Zero

**Mô tả**: Tự động quét và dọn dẹp email quảng cáo, email đã đọc hoặc thông báo hệ thống cũ hơn N ngày.

**Actors**: Personal User, System (Background Job)

**Preconditions**:
- User đã đăng nhập và kết nối Google Account
- User đã cấu hình cleanup rules (categories, thời gian threshold)

**Flow chính**:
1. Background Job chạy theo lịch (configurable: mỗi giờ / mỗi ngày)
2. Hệ thống query Gmail API lấy danh sách email theo filter rules
3. Phân loại email: Promotions, Social, System Notifications, Read > N days
4. Với mỗi email khớp rule → chuyển vào Trash hoặc Archive
5. Ghi log kết quả vào database (số email đã xử lý, categories)
6. Gửi notification tóm tắt cho user qua Dashboard / SignalR

**Luồng thay thế**:
- User có thể chạy manual cleanup từ Dashboard
- User có thể whitelist sender/domain để không bị cleanup
- User có thể undo (untrash) trong vòng 30 ngày

**Business Rules**:
- Mặc định KHÔNG xóa email chưa đọc (trừ Promotions)
- Email có star/flag luôn được bảo vệ
- Tối đa xử lý 500 email/lần để tránh rate limit
- Rate limit: max 250 requests/giây (Gmail API quota)

**Data Model**:
```
CleanupRule {
  Id, UserId, RuleName, Category, 
  OlderThanDays, Action (Trash/Archive),
  WhitelistDomains[], IsActive, CreatedAt
}

CleanupLog {
  Id, UserId, RuleId, ExecutedAt,
  TotalProcessed, TotalTrashed, TotalArchived
}
```

---

### UC02 — Human-in-the-Loop AI Mail Reply

**Mô tả**: AI nhận diện email cần phản hồi → sinh bản nháp → người dùng duyệt trước khi gửi.

**Actors**: Personal User, AI Service, System

**Preconditions**:
- User đã kết nối Google Account
- User đã bật tính năng AI Draft
- AI API key đã được cấu hình

**Flow chính**:
1. Hệ thống scan inbox tìm email chưa reply (configurable criteria)
2. AI phân tích nội dung email và xác định mức độ cần reply
3. AI sinh bản nháp reply (tone, language theo user preference)
4. Tạo Draft trên Gmail API (chưa gửi)
5. Push notification đến Dashboard qua SignalR
6. User xem draft trên Dashboard → **Approve / Edit / Reject**
7. Nếu Approve → gửi email qua Gmail API
8. Nếu Edit → user chỉnh sửa → gửi
9. Nếu Reject → xóa draft, ghi log

**Business Rules**:
- AI **KHÔNG BAO GIỜ** tự động gửi email — luôn cần user approval
- Confidence score hiển thị cho user (AI đánh giá % phù hợp)
- Giữ lại lịch sử drafts để AI học từ feedback
- Hỗ trợ multi-language reply (VI, EN)
- Template-based: user có thể tạo reply templates cho AI tham khảo

**Data Model**:
```
AIDraft {
  Id, UserId, OriginalEmailId, GmailDraftId,
  DraftContent, ConfidenceScore, Status (Pending/Approved/Rejected/Edited),
  UserFeedback, CreatedAt, ProcessedAt
}

ReplyTemplate {
  Id, UserId, TemplateName, Category, Content, Language
}
```

---

### UC07 — Auto Attachment Quarantine & Organization

**Priority**: 🟡 Should-Have

**Mô tả**: Tự động bóc tách file đính kèm từ Gmail, phân loại và lưu vào Drive theo cấu trúc `/Attachments/YYYY-MM/Category/`.

**Flow chính**:
1. Background job scan email mới có attachment
2. Download attachment qua Gmail API
3. Phân loại file theo extension (PDF → Documents, XLSX → Spreadsheets, ...)
4. Upload lên Drive theo folder structure
5. Ghi metadata log (email source, filename, size, category)

**Business Rules**:
- Chỉ xử lý file > 100KB (skip inline images nhỏ)
- Duplicate detection bằng file hash (MD5)
- Max file size: 25MB (Gmail limit)
- Folder structure: `/G-Ops Attachments/YYYY-MM/{Category}/`

---

### UC08 — Executive Email Summarizer

**Priority**: 🟢 Nice-to-Have

**Mô tả**: Tự động tổng hợp email threads dài thành tài liệu tóm tắt trên Google Docs.

**Flow chính**:
1. Detect email thread có > N messages (configurable, default 5)
2. AI đọc và tổng hợp: Key Decisions, Action Items, Deadlines
3. Tạo Google Docs document với summary
4. Link document với email thread trong Dashboard

---

## Module 2: Scheduling (Calendar Sync)

### UC03 — AI Schedule Extractor

**Mô tả**: Tự động phát hiện email chứa lịch hẹn → AI trích xuất thời gian/địa điểm → tạo Event trên Calendar.

**Actors**: Personal User, AI Service, System

**Preconditions**:
- User đã kết nối Google Account (Gmail + Calendar scope)
- AI extraction đã bật

**Flow chính**:
1. Email mới đến → hệ thống scan nội dung
2. AI classify: có chứa schedule info? (interview, flight, meeting, ...)
3. AI extract: DateTime, Location, Title, Duration, Attendees
4. Kiểm tra conflict với existing Calendar events
5. Tạo Google Calendar Event + set Reminder (configurable)
6. Notify user qua Dashboard

**Loại email hỗ trợ**:
- Lịch phỏng vấn (interview scheduling)
- Vé máy bay / tàu (booking confirmations)
- Lịch họp nhóm đồ án
- Hẹn gặp / Appointment confirmations

**Business Rules**:
- Duplicate detection: không tạo event trùng (same time + title)
- Timezone handling: auto-detect từ email content hoặc user setting
- Confidence threshold: chỉ tự động tạo khi confidence > 80%, ngược lại yêu cầu user confirm

**Data Model**:
```
ExtractedSchedule {
  Id, UserId, SourceEmailId, Title, StartTime, EndTime,
  Location, Description, CalendarEventId,
  ConfidenceScore, Status (AutoCreated/PendingConfirm/Confirmed/Rejected),
  CreatedAt
}
```

---

## Module 3: Finance (Sheets Telemetry)

### UC04 — Automated Transaction Logging

**Mô tả**: Nhận diện email biến động số dư → bóc tách số tiền → ghi log vào Google Sheets.

**Actors**: Personal User, System

**Flow chính**:
1. Email từ ngân hàng/ví điện tử đến (VCB, TCB, MBBank, Momo, ZaloPay, ...)
2. AI parse: Số tiền, Loại GD (Credit/Debit), Nội dung, Thời gian, Số dư
3. Classify: Thu/Chi, Category (ăn uống, di chuyển, lương, ...)
4. Nếu giao dịch < 500.000 VNĐ → tự động mark as read
5. Ghi dòng mới vào Google Sheets (append row)
6. Cập nhật Dashboard tổng hợp thu chi

**Supported Banks/Wallets**:
- Vietcombank (VCB)
- Techcombank (TCB)
- MB Bank
- VPBank
- Momo
- ZaloPay

**Sheets Structure**:
```
| Mã GD | Thời gian | Ngân hàng | Loại GD | Số tiền | Phí | TK trích | TK ghi | Tên người hưởng | Danh mục | Nội dung |
```

**Business Rules**:
- Extraction đầy đủ 11 trường thông tin giao dịch qua Gemini AI (Mã GD, Thời gian, Ngân hàng, Loại GD, Số tiền, Phí, TK trích, TK ghi, Tên người hưởng, Danh mục, Nội dung).
- Duplicate detection qua EmailId / MessageId.
- Auto-categorization bằng keyword matching + AI fallback.
- Tự động tạo file Google Sheet mới theo tháng (`BaoCaoTaiChinh_{yyyy_MM}`) và di chuyển vào Thư mục Google Drive (Folder ID) được cấu hình.
- Tự động nhận diện chính xác tên Sheet tab đầu tiên (ví dụ: `'Trang tính1'` hay `'Sheet1'`) để chèn hàng dữ liệu mới (`INSERT_ROWS`).

---

## Module 4: Drive Guard

### UC05 — Real-time Change Audit

**Mô tả**: Theo dõi thư mục dùng chung, ghi log mọi tác động (Create, Edit, Delete, Trash).

**Flow chính**:
1. Register Drive API watch channel cho monitored folders
2. Nhận webhook khi có thay đổi
3. Query Drive API để lấy chi tiết change
4. Ghi audit log: Who, What, When, File, Action
5. Real-time update Dashboard qua SignalR
6. Alert nếu có bulk delete (> 5 files trong 1 phút)

**Data Model**:
```
DriveAuditLog {
  Id, UserId, FolderId, FileId, FileName,
  Action (Created/Modified/Deleted/Trashed/Moved/Shared),
  ActorEmail, ActorName, Timestamp, Details
}

MonitoredFolder {
  Id, UserId, FolderId, FolderName, IsActive,
  AlertOnBulkDelete, BulkDeleteThreshold
}
```

---

### UC06 — Suspicious File Guard

**Mô tả**: Cảnh báo khi phát hiện file có định dạng nguy hiểm được upload vào thư mục chung.

**Dangerous Extensions**: `.exe`, `.bat`, `.vbs`, `.cmd`, `.ps1`, `.scr`, `.msi`, `.dll`, `.zip` (unknown source), `.7z`, `.rar`

**Flow chính**:
1. File mới được tạo/upload trong monitored folder (từ UC05 webhook)
2. Check file extension + MIME type
3. Nếu khớp dangerous list → tạo Alert (High/Critical severity)
4. Notify user ngay lập tức qua SignalR + Email notification
5. Option: Auto-quarantine (move to quarantine folder)

---

### UC09 — Permission Drift Monitor

**Priority**: 🟡 Should-Have

**Mô tả**: Quét quyền truy cập, cảnh báo khi file bị chuyển từ Private → Public.

**Flow chính**:
1. Scheduled job quét permissions của monitored files/folders
2. So sánh với snapshot quyền trước đó
3. Detect drift: Private → Anyone with link / Public
4. Alert user + hiển thị 1-click Revoke Permission trên Dashboard

---

## Module 5: Backup

### UC10 — Snapshot & Backup Engine

**Priority**: 🟡 Should-Have

**Mô tả**: Định kỳ xuất dữ liệu thu chi, audit log ra CSV/JSON → lưu vào Drive.

**Flow chính**:
1. Scheduled job (weekly/monthly)
2. Export data từ Sheets + Database
3. Generate CSV/JSON files
4. Upload vào `/G-Ops Backups/YYYY-MM/` trên Drive
5. Notify user khi backup hoàn tất

---

## Module 6: Workflow Automation

### UC11 — Form Response Processing

**Priority**: 🟢 Nice-to-Have

**Mô tả**: Xử lý phản hồi Google Forms → phân loại, gửi email xác nhận, tạo Calendar event.

---

### UC12 — Multi-Channel Alerting

**Priority**: 🟡 Should-Have

**Mô tả**: Đẩy thông báo real-time sang Google Chat / Discord khi có sự kiện quan trọng.

**Supported Channels**:
- Dashboard (SignalR) — mặc định
- Email notification
- Discord Webhook
- Google Chat Webhook (future)

**Alert Types**:
- 🔴 Critical: Suspicious file detected, Permission drift
- 🟡 Warning: Bulk delete, AI draft needs review
- 🔵 Info: Cleanup completed, Transaction logged, Backup done
