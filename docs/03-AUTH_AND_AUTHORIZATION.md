# 🔐 G-Ops Hub — Authentication & Authorization

> Thiết kế hệ thống xác thực đơn giản: 1 Admin + Anonymous viewers.

---

## 1. Mô hình người dùng

Hệ thống G-Ops Hub là **dự án cá nhân** nên authentication được đơn giản hóa tối đa:

| Role | Ai? | Cách truy cập | Quyền |
|------|-----|----------------|-------|
| **Admin** | `hnt.vn.vn@gmail.com` (chỉ duy nhất) | Google Sign-In | Full access tất cả tính năng |
| **Anonymous** | Bất kỳ ai | Truy cập trực tiếp (không login) | Chỉ xem thông tin public |

> **Không có registration, không có multi-user.** Chỉ 1 admin duy nhất.

---

## 2. Authentication Flow

### 2.1 Admin Login Flow

```
Admin → [Login Page] → [Google Sign-In Button]
                            │
                            ▼
                  [Google OAuth 2.0 Consent Screen]
                  Scopes: openid, profile, email,
                  gmail.modify, calendar.events,
                  drive.file, spreadsheets
                            │
                            ▼
                  [Backend: /api/v1/auth/google-callback]
                  1. Verify Google ID Token
                  2. Kiểm tra email === "hnt.vn.vn@gmail.com"
                     → Nếu KHÔNG khớp → Reject (403 Forbidden)
                     → Nếu khớp → Continue
                  3. Lưu/cập nhật Google Refresh Token (encrypted)
                  4. Issue JWT Access Token
                            │
                            ▼
                  [Frontend: Store JWT in httpOnly cookie]
                  → Redirect to Dashboard (full access)
```

### 2.2 Anonymous Access

```
Anyone → [Truy cập G-Ops Hub URL]
              │
              ▼
         [Frontend SPA loads]
         Không có JWT cookie → Anonymous mode
              │
              ▼
         [Chỉ hiển thị public views]
         - Lịch bận (Busy/Free calendar)
         - System status
         - Không thể xem email, finance, drive details
```

### 2.3 Token Strategy

| Token | Lifetime | Storage | Purpose |
|-------|----------|---------|---------|
| JWT Access Token | 1 giờ | httpOnly cookie | API authentication |
| JWT Refresh Token | 30 ngày | httpOnly cookie + MongoDB | Renew JWT |
| Google Access Token | 1 giờ | Server-side (encrypted in DB) | Google API calls |
| Google Refresh Token | Không hết hạn* | Server-side (encrypted in DB) | Renew Google AT |

> Vì chỉ có 1 user, token lifetime có thể dài hơn bình thường (1h access, 30d refresh).

---

## 3. Google OAuth Scopes

```csharp
public static class GoogleScopes
{
    // Base
    public const string OpenId = "openid";
    public const string Profile = "profile";
    public const string Email = "email";
    
    // Gmail
    public const string GmailModify = "https://www.googleapis.com/auth/gmail.modify";
    
    // Calendar
    public const string CalendarEvents = "https://www.googleapis.com/auth/calendar.events";
    
    // Drive
    public const string DriveFile = "https://www.googleapis.com/auth/drive.file";
    public const string DriveReadonly = "https://www.googleapis.com/auth/drive.readonly";
    
    // Sheets
    public const string Spreadsheets = "https://www.googleapis.com/auth/spreadsheets";
    
    // Docs (UC08 - future)
    public const string DocsReadwrite = "https://www.googleapis.com/auth/documents";
}
```

---

## 4. Authorization — Đơn giản hóa

### 4.1 API Endpoint Protection

```
[AllowAnonymous] endpoints:
  GET /api/v1/public/calendar    → Lịch bận/rảnh
  GET /api/v1/public/status      → System health
  POST /api/v1/auth/google-login → Login flow

[Authorize] endpoints (Admin only):
  Tất cả endpoints còn lại
```

### 4.2 Implementation

```csharp
// Program.cs - Simplified auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT config */ });

// Middleware - Check admin email
public class AdminOnlyAuthorizationHandler : AuthorizationHandler<AdminRequirement>
{
    private const string AdminEmail = "hnt.vn.vn@gmail.com";
    
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
        if (email == AdminEmail)
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
```

### 4.3 Public vs Protected Views (Frontend)

```typescript
// Vue Router guards
const routes = [
  // Public routes - anyone can access
  { path: '/login', component: LoginView },
  { path: '/public/calendar', component: PublicCalendarView },
  
  // Protected routes - Admin only
  { 
    path: '/',
    component: DefaultLayout,
    meta: { requiresAuth: true },
    children: [
      { path: 'dashboard', component: DashboardView },
      { path: 'email', component: EmailOpsView },
      { path: 'finance', component: FinanceView },
      { path: 'drive', component: DriveGuardView },
      { path: 'settings', component: SettingsView },
      // ... all admin features
    ]
  }
];

// Navigation guard
router.beforeEach((to) => {
  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return '/login';
  }
});
```

---

## 5. Security

### 5.1 Measures

- JWT in **httpOnly, Secure, SameSite=Lax** cookies (Lax vì cross-origin Vercel → MonsterASP)
- Google tokens **encrypted at rest** trong MongoDB
- Admin email **hardcoded** — không thể thay đổi qua API
- CORS: chỉ cho phép Vercel frontend domain
- Rate limiting trên login endpoint (chống brute force)

### 5.2 Vercel → MonsterASP Proxy

```jsonc
// vercel.json
{
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "http://your-app.monsterasp.net/api/:path*"
    }
  ]
}
```

Cách này giải quyết:
- Mixed content (HTTPS frontend → HTTP backend)
- CORS issues (same-origin qua proxy)
- Cookie sharing (same domain)

### 5.3 User Data Model (MongoDB)

```json
// Collection: admin_user (chỉ có 1 document)
{
  "_id": "ObjectId",
  "email": "hnt.vn.vn@gmail.com",
  "displayName": "Thien HN",
  "avatarUrl": "https://...",
  "googleId": "google-unique-id",
  "googleAccessToken": "encrypted-...",
  "googleRefreshToken": "encrypted-...",
  "googleTokenExpiresAt": "ISODate",
  "settings": {
    "cleanupEnabled": true,
    "cleanupSchedule": "0 0 * * *",
    "aiDraftEnabled": true,
    "aiDraftLanguage": "vi",
    "scheduleExtractionEnabled": true,
    "transactionLoggingEnabled": true,
    "timezone": "Asia/Ho_Chi_Minh",
    "language": "vi",
    "notificationChannels": {
      "dashboard": true,
      "discord": "https://discord.com/api/webhooks/..."
    }
  },
  "lastLoginAt": "ISODate",
  "createdAt": "ISODate",
  "updatedAt": "ISODate"
}
```
