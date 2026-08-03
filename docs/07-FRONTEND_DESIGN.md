# 🎨 G-Ops Hub — Frontend Design

> Thiết kế giao diện Dashboard cho hệ thống G-Ops Hub (Vue 3).

---

## 1. Design System

### 1.1 Color Palette

```scss
// Dark Theme (Primary)
$bg-primary: #0F1117;       // Main background
$bg-secondary: #1A1D27;     // Card background
$bg-tertiary: #242836;      // Hover / Active state
$border-color: #2E3345;     // Borders

$text-primary: #F1F3F9;     // Main text
$text-secondary: #8B92A8;   // Secondary text
$text-muted: #5A6178;       // Disabled / hint

// Accent Colors
$accent-blue: #4F8EF7;      // Primary action
$accent-green: #34D399;     // Success / Income
$accent-red: #F87171;       // Danger / Expense
$accent-yellow: #FBBF24;    // Warning
$accent-purple: #A78BFA;    // AI / Smart features
$accent-cyan: #22D3EE;      // Info

// Severity Colors
$severity-critical: #EF4444;
$severity-high: #F97316;
$severity-warning: #EAB308;
$severity-info: #3B82F6;
```

### 1.2 Typography

```scss
// Font family: Inter (Google Fonts)
$font-primary: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
$font-mono: 'JetBrains Mono', 'Fira Code', monospace;

$font-size-xs: 0.75rem;   // 12px
$font-size-sm: 0.875rem;  // 14px
$font-size-base: 1rem;    // 16px
$font-size-lg: 1.125rem;  // 18px
$font-size-xl: 1.25rem;   // 20px
$font-size-2xl: 1.5rem;   // 24px
$font-size-3xl: 2rem;     // 32px
```

### 1.3 Spacing & Border Radius

```scss
$radius-sm: 6px;
$radius-md: 10px;
$radius-lg: 16px;
$radius-xl: 20px;

$spacing: 4px 8px 12px 16px 20px 24px 32px 40px 48px;
```

---

## 2. Layout Structure

```
┌─────────────────────────────────────────────────────────┐
│  Header (64px)                                          │
│  Logo │ Search │ Notifications Bell │ User Avatar       │
├──────────┬──────────────────────────────────────────────┤
│          │                                              │
│ Sidebar  │  Main Content Area                           │
│ (240px)  │                                              │
│          │  ┌──────────────────────────────────────────┐│
│ 📊 Dashboard│ │  Page Header + Breadcrumb              ││
│ ✉️ Email    │ │                                        ││
│ 📅 Calendar │ │  Content                               ││
│ 💰 Finance  │ │                                        ││
│ 🛡️ Drive    │ │                                        ││
│ ⚙️ Settings │ └──────────────────────────────────────────┘│
│ 👤 Admin    │                                              │
│          │                                              │
├──────────┴──────────────────────────────────────────────┤
│  (No footer — infinite scroll / sticky sidebar)         │
└─────────────────────────────────────────────────────────┘
```

---

## 3. Page Designs

### 3.1 Dashboard View

```
┌─────────────────────────────────────────────────────────┐
│  📊 Dashboard                              Good Morning!│
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐      │
│  │✉️ 47    │ │📝 3     │ │💰 +2.5M │ │🛡️ 1     │      │
│  │Cleaned  │ │Pending  │ │Balance  │ │Alert    │      │
│  │Today    │ │Drafts   │ │This Mon │ │Active   │      │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘      │
│                                                         │
│  ┌────────────────────────┐ ┌──────────────────────┐    │
│  │ 📈 Income vs Expense   │ │ 🔔 Recent Activity   │    │
│  │    (ECharts Line)      │ │                      │    │
│  │                        │ │  • AI Draft created  │    │
│  │                        │ │  • 12 emails cleaned │    │
│  │                        │ │  • Transaction logged│    │
│  │                        │ │  • Meeting extracted │    │
│  └────────────────────────┘ └──────────────────────┘    │
│                                                         │
│  ┌────────────────────────┐ ┌──────────────────────┐    │
│  │ 🥧 Spending Categories │ │ ⚡ Quick Actions      │    │
│  │   (Pie Chart)          │ │                      │    │
│  │                        │ │  [Run Cleanup]       │    │
│  │                        │ │  [Trigger Backup]    │    │
│  │                        │ │  [Scan Permissions]  │    │
│  └────────────────────────┘ └──────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

### 3.2 Email Operations View

```
┌─────────────────────────────────────────────────────────┐
│  ✉️ Email Operations                                    │
│  [Cleanup Rules] [AI Drafts] [Cleanup History]   tabs   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Tab: AI Drafts (Pending Review)                        │
│                                                         │
│  ┌──────────────────────────────────────────────────┐   │
│  │  📧 From: professor@uni.edu                      │   │
│  │  Subject: RE: Đồ án cuối kỳ - Deadline          │   │
│  │  Confidence: 87% ████████░░                      │   │
│  │                                                  │   │
│  │  AI Draft:                                       │   │
│  │  ┌────────────────────────────────────────────┐  │   │
│  │  │ Chào Thầy,                                 │  │   │
│  │  │ Em xin phép xác nhận đã nhận được email... │  │   │
│  │  │ Em sẽ hoàn thành trước ngày 15/08...       │  │   │
│  │  └────────────────────────────────────────────┘  │   │
│  │                                                  │   │
│  │  [✅ Approve & Send] [✏️ Edit] [❌ Reject]       │   │
│  └──────────────────────────────────────────────────┘   │
│                                                         │
│  ┌──────────────────────────────────────────────────┐   │
│  │  📧 From: hr@company.com                         │   │
│  │  Subject: Interview Schedule Confirmation        │   │
│  │  Confidence: 92% █████████░                      │   │
│  │  ...                                             │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

### 3.3 Finance View

```
┌─────────────────────────────────────────────────────────┐
│  💰 Finance Tracker           [Aug 2026 ▼] [Export CSV] │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐              │
│  │ +5.2M    │  │ -3.8M    │  │ +1.4M    │              │
│  │ Thu nhập │  │ Chi tiêu │  │ Chênh lệch│             │
│  └──────────┘  └──────────┘  └──────────┘              │
│                                                         │
│  ┌──────────────────────────────────────────────────┐   │
│  │ Transaction History                   [Filter ▼] │   │
│  ├──────┬──────┬────────┬─────────┬────────┬───────┤   │
│  │ Ngày │ Bank │ Loại   │ Số tiền │ Nội d. │ D.mục │   │
│  ├──────┼──────┼────────┼─────────┼────────┼───────┤   │
│  │08/03 │ VCB  │🟢 Thu  │+5,000K  │Lương T8│Lương  │   │
│  │08/03 │ Momo │🔴 Chi  │ -150K   │Grab    │Di chuy│   │
│  │08/02 │ TCB  │🔴 Chi  │ -85K    │Bách Hóa│Ăn uống│   │
│  │08/02 │ VCB  │🔴 Chi  │ -200K   │Điện    │Hóa đơn│   │
│  └──────┴──────┴────────┴─────────┴────────┴───────┘   │
└─────────────────────────────────────────────────────────┘
```

### 3.4 Drive Guard View

```
┌─────────────────────────────────────────────────────────┐
│  🛡️ Drive Guard                                        │
│  [Audit Log] [Alerts] [Monitored Folders] [Permissions] │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Tab: Alerts                                            │
│                                                         │
│  ┌─ 🔴 CRITICAL ──────────────────────────────────┐     │
│  │  ⚠️ Suspicious file detected                   │     │
│  │  File: virus_scanner.exe                        │     │
│  │  Folder: /Shared/Project-Alpha/                 │     │
│  │  Uploaded by: unknown@gmail.com                 │     │
│  │  Time: 2 minutes ago                            │     │
│  │                                                 │     │
│  │  [🗑️ Quarantine] [✅ Mark Safe] [👁️ Details]    │     │
│  └─────────────────────────────────────────────────┘     │
│                                                         │
│  ┌─ 🟡 WARNING ───────────────────────────────────┐     │
│  │  🔓 Permission drift detected                  │     │
│  │  File: Budget_Q3_2026.xlsx                      │     │
│  │  Changed: Private → Anyone with link            │     │
│  │  Changed by: member@team.com                    │     │
│  │                                                 │     │
│  │  [🔒 Revoke Public Access] [✅ Acknowledge]     │     │
│  └─────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────┘
```

---

## 4. Vue Router Structure

```typescript
const routes = [
  {
    path: '/login',
    component: AuthLayout,
    children: [
      { path: '', name: 'login', component: LoginView }
    ]
  },
  {
    path: '/',
    component: DefaultLayout,
    meta: { requiresAuth: true },
    children: [
      { path: '', redirect: '/dashboard' },
      { path: 'dashboard', name: 'dashboard', component: DashboardView },
      { path: 'email', name: 'email-ops', component: EmailOpsView },
      { path: 'calendar', name: 'calendar', component: CalendarView },
      { path: 'finance', name: 'finance', component: FinanceView },
      { path: 'drive', name: 'drive-guard', component: DriveGuardView },
      { path: 'settings', name: 'settings', component: SettingsView },
    ]
  },
  {
    path: '/admin',
    component: AdminLayout,
    meta: { requiresAuth: true, requiresAdmin: true },
    children: [
      { path: '', name: 'admin', component: AdminView },
      { path: 'users', name: 'admin-users', component: UserManagementView },
      { path: 'jobs', name: 'admin-jobs', component: JobMonitorView },
      { path: 'system', name: 'admin-system', component: SystemSettingsView },
    ]
  },
  { path: '/:pathMatch(.*)*', component: NotFoundView }
];
```

---

## 5. Responsive Breakpoints

```scss
$breakpoint-sm: 640px;   // Mobile
$breakpoint-md: 768px;   // Tablet
$breakpoint-lg: 1024px;  // Desktop
$breakpoint-xl: 1280px;  // Wide
$breakpoint-2xl: 1536px; // Ultra-wide
```

- **Mobile (< 768px)**: Sidebar collapses to hamburger menu
- **Tablet (768-1024px)**: Sidebar collapses to icons only
- **Desktop (> 1024px)**: Full sidebar with labels
