# 💡 G-Ops Hub — Additional Features & Recommendations

> Đề xuất các tính năng bổ sung nâng cao hệ thống G-Ops Hub.

---

## 1. Tính năng đề xuất bổ sung

### ⭐ UC13 — Smart Email Priority Scoring

**Priority**: Should-Have

**Mô tả**: AI tự động chấm điểm ưu tiên cho mỗi email đến (1-10) dựa trên sender, nội dung, urgency keywords, deadline mentions. Giúp user tập trung vào email quan trọng nhất.

**Giá trị**:
- Giảm thời gian scan inbox 60-70%
- Focus vào email có deadline/action required
- Tự động pin email priority cao

---

### ⭐ UC14 — Recurring Report Generator

**Priority**: Should-Have

**Mô tả**: Tự động sinh báo cáo tổng hợp hàng tuần/tháng:
- Email stats: Bao nhiêu email đã cleanup, draft approved/rejected
- Finance: Tổng thu, tổng chi, top categories
- Drive: Bao nhiêu changes, alerts resolved
- Gửi report qua email hoặc tạo Google Docs

**Giá trị**: Cung cấp tầm nhìn tổng quan (executive overview) mà không cần mở Dashboard.

---

### ⭐ UC15 — Custom Automation Rules (IFTTT-style)

**Priority**: Nice-to-Have

**Mô tả**: Cho phép user tạo automation rules dạng "If This Then That":
- **IF** email from `@bank.com` **THEN** log transaction + mark as read
- **IF** Drive file deleted by non-owner **THEN** alert + restore
- **IF** Calendar event tomorrow **THEN** send Discord reminder

**Giá trị**: Biến hệ thống thành truly extensible automation platform.

---

### ⭐ UC16 — Activity Analytics & Insights Dashboard

**Priority**: Should-Have

**Mô tả**: Dashboard phân tích sâu:
- Email response time trung bình
- Thời điểm nhận email nhiều nhất
- Spending trends over time (so sánh tháng trước)
- Drive storage usage trends
- Heatmap hoạt động theo ngày/giờ

**Giá trị**: Data-driven insights cho việc quản lý thời gian và tài chính.

---

### ⭐ UC17 — Email Template & Snippet Manager

**Priority**: Nice-to-Have

**Mô tả**: Quản lý template email thường dùng (không chỉ AI reply). User có thể lưu, phân loại, và quick-insert templates khi soạn email.

---

### ⭐ UC18 — Google Tasks Integration

**Priority**: Nice-to-Have

**Mô tả**: AI extract action items từ email → tự động tạo Google Tasks hoặc hiển thị TODO list trên Dashboard.

---

## 2. Non-Functional Requirements bổ sung

### 2.1 Performance

| Metric | Target |
|--------|--------|
| API response time (p95) | < 500ms |
| Dashboard load time | < 2s |
| Background job throughput | 500 emails/phút |
| SignalR latency | < 200ms |
| Database query time (p95) | < 100ms |

### 2.2 Scalability

- Horizontal scaling: Stateless API → có thể scale N instances
- Redis cluster cho distributed cache
- Database connection pooling (max 100 connections)
- Background job partitioning per user

### 2.3 Availability

- Target: 99.5% uptime
- Health check endpoints cho monitoring
- Auto-restart on failure (Docker restart policy)
- Graceful degradation: nếu Google API down → queue jobs, retry later

### 2.4 Security

- **OWASP Top 10** compliance
- SQL Injection prevention (EF Core parameterized queries)
- XSS prevention (Vue auto-escaping + CSP headers)
- CSRF protection
- Input sanitization
- Audit logging cho mọi admin action
- Google token encryption at rest
- Rate limiting per endpoint + per user

### 2.5 Observability

- **Structured Logging**: Serilog → Seq/ELK
- **Health Checks**: `/api/health` (DB, Redis, Google API connectivity)
- **Metrics**: Request count, latency, error rate
- **Alerting**: Notify when error rate > threshold

### 2.6 Accessibility (a11y)

- WCAG 2.1 AA compliance
- Keyboard navigation
- Screen reader support
- Color contrast ratios > 4.5:1
- Focus indicators

### 2.7 Internationalization (i18n)

- Vietnamese (default)
- English
- Date/time format per locale
- Currency format per locale (VNĐ, USD)

---

## 3. Technical Debt & Risk Mitigation

### 3.1 Google API Rate Limits

| API | Quota | Mitigation |
|-----|-------|-----------|
| Gmail | 250 units/sec/user | Batch requests, exponential backoff |
| Calendar | 500 requests/100sec | Request dedup, caching |
| Drive | 12,000 requests/100sec | Webhook-based (push), not polling |
| Sheets | 300 requests/min | Batch writes, buffer |

### 3.2 AI Cost Management

- Cache AI responses cho similar emails
- Confidence threshold: skip AI processing for obvious cases
- Token limit per request
- Monthly usage tracking + alerts

### 3.3 Data Privacy

- User data isolated by UserId
- GDPR-like practices: right to deletion
- No storing email content long-term (chỉ metadata + AI-generated content)
- Encrypted tokens
- Clear data retention policy (e.g., logs > 90 days → auto-delete)

---

## 4. Updated Requirements Matrix

| Module | UC ID | Tên | Priority | Phase |
|--------|-------|-----|----------|-------|
| Email Ops | UC01 | Auto-Clean Inbox | 🔴 Must-Have | v0.2 |
| Email Ops | UC02 | AI Draft (Human-in-Loop) | 🔴 Must-Have | v0.2 |
| Email Ops | UC07 | Attachment Quarantine | 🟡 Should-Have | v1.1 |
| Email Ops | UC08 | Thread Summarizer | 🟢 Nice-to-Have | v1.2 |
| Email Ops | UC13 | Priority Scoring | 🟡 Should-Have | v1.1 |
| Scheduling | UC03 | Schedule Extractor | 🔴 Must-Have | v0.3 |
| Finance | UC04 | Transaction Logging | 🔴 Must-Have | v0.4 |
| Drive Guard | UC05 | Folder Audit | 🔴 Must-Have | v0.5 |
| Drive Guard | UC06 | File Guard | 🔴 Must-Have | v0.5 |
| Drive Guard | UC09 | Permission Monitor | 🟡 Should-Have | v1.1 |
| Backup | UC10 | Backup Engine | 🟡 Should-Have | v1.1 |
| Workflow | UC11 | Form Processing | 🟢 Nice-to-Have | v1.2 |
| Alerting | UC12 | Multi-Channel Alert | 🟡 Should-Have | v1.1 |
| Analytics | UC14 | Report Generator | 🟡 Should-Have | v1.1 |
| Automation | UC15 | Custom Rules | 🟢 Nice-to-Have | v1.2 |
| Analytics | UC16 | Activity Insights | 🟡 Should-Have | v1.1 |
| Email Ops | UC17 | Template Manager | 🟢 Nice-to-Have | v1.2 |
| Tasks | UC18 | Tasks Integration | 🟢 Nice-to-Have | v1.2 |

---

## 5. Tổng kết đề xuất

Với 18 Use Cases, hệ thống được chia thành:
- **6 Must-Have** (UC01-06) → MVP v1.0
- **7 Should-Have** (UC07, UC09, UC10, UC12-14, UC16) → v1.1
- **5 Nice-to-Have** (UC08, UC11, UC15, UC17, UC18) → v1.2+

Bộ docs hiện tại bao gồm:

| # | Document | Nội dung |
|---|----------|----------|
| 01 | PROJECT_OVERVIEW.md | Vision, Tech Stack, Architecture, Roadmap |
| 02 | USE_CASES.md | 12 Use Cases chi tiết |
| 03 | AUTH_AND_AUTHORIZATION.md | OAuth, RBAC, Permission Matrix, Security |
| 04 | PROJECT_STRUCTURE.md | Folder structure (Clean Architecture) |
| 05 | DATABASE_DESIGN.md | ERD, Entities, Enums, Indexes |
| 06 | API_SPECIFICATION.md | RESTful API endpoints, SignalR events |
| 07 | FRONTEND_DESIGN.md | Design system, Wireframes, Router |
| 08 | DEPLOYMENT_GUIDE.md | Dev setup, Docker, CI/CD |
| 09 | ADDITIONAL_FEATURES.md | 6 UC mới + NFR + Risk (file này) |
