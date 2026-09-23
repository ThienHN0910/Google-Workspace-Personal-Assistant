# ⚡ G-Ops Hub — Google Workspace Operations & AI Personal Assistant

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Vue 3](https://img.shields.io/badge/Vue.js-3.x-4FC08D?logo=vuedotjs&logoColor=white)](https://vuejs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178C6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![Tests](https://img.shields.io/badge/Tests-120%2F120_Passing-brightgreen?logo=checkmarx&logoColor=white)](https://github.com/ThienHN0910/Google-Workspace-Personal-Assistant)
[![MongoDB Atlas](https://img.shields.io/badge/MongoDB-Atlas_M0-47A248?logo=mongodb&logoColor=white)](https://www.mongodb.com/)
[![Google Gemini AI](https://img.shields.io/badge/AI-Google_Gemini-8E75B2?logo=google&logoColor=white)](https://ai.google.dev/)
[![Case Study](https://img.shields.io/badge/Case_Study-Portfolio-007ACC?style=flat-square&logo=vercel)](https://thienhn0910.vercel.app/projects/google-workspace-personal-assistant)
[![Blog](https://img.shields.io/badge/Blog-Architecture_Deep_Dive-orange?style=flat-square)](https://thienhn0910.vercel.app/blog/architecting-g-ops-hub-dotnet-cqrs-hangfire)
[![Author](https://img.shields.io/badge/Author-ThienHN-4FC08D?style=flat-square)](https://thienhn0910.vercel.app/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

> **G-Ops Hub** is an intelligent, AI-powered personal operations engine built for the Google Workspace ecosystem (Gmail, Calendar, Drive, Sheets, Tasks). It automates repetitive operational workflows, enforces Drive asset security, logs financial transactions, and provides interactive scheduling dashboards with Telegram ChatOps. Engineered by [ThienHN](https://thienhn0910.vercel.app/).

---

## 🌟 Key Features

### ⚙️ Background Automation Hub (Hangfire Engine)
- **4 Scheduled Recurring Jobs** running server-side with zero manual intervention:
  - **Drive Guard Audit (`drive-guard-audit`)**: Scans monitored folders every **50 minutes**; detects suspicious executables (`.exe`, `.bat`, `.zip`, `.scr`) and alerts on bulk deletion (**>= 3 files**).
  - **Bank Telemetry Sync (`bank-telemetry`)**: Scans bank balance fluctuation emails every **30 minutes**, parses financial records via Gemini AI, and syncs to Google Sheets.
  - **Regex-First Email Cleanup (`email-cleanup`)**: Sweeps inbox every **12 hours** using regex patterns first, followed by AI pattern learning for recurring spam.
  - **Smart Calendar Extractor (`calendar-extractor`)**: Scans incoming communications every **2 hours** for interview invitations and schedule requests.
- **Dynamic Hangfire Rescheduling**: Changing intervals in the Settings Hub immediately reschedules Hangfire cron jobs without requiring a backend server restart.

### 🧹 Regex-First EmailOps & AI Rule Learning (UC01)
- **Two-Phase Sweep**:
  - *Phase 1 (Regex-First)*: Automatically cleans known commercial newsletters and promotional notifications using compiled regex patterns, consuming **0 AI tokens**.
  - *Phase 2 (AI Pattern Learning)*: Uses Gemini AI to analyze remaining spam patterns, automatically generates and deduplicates new regex rules, saves them as pending or active rules, and alerts via Telegram.
- **Trash vs. Archive Action Support**: Rules support configurable target actions (`Trash` for decluttering inbox with recoverable 30-day Google Trash retention, or `Archive` for long-term storage).
- **ReDoS Safety & Protection**: Regex execution is protected with strict timeouts (500ms) and compile validations to prevent catastrophic backtracking.
- **Protected Financial & Tech Domains**: Strictly protects emails from VPBank, Vietcombank, Techcombank, MB Bank, MoMo, GitHub, Google, Vercel, and custom whitelist domains against accidental deletion.
- **Immutable Audit Logging**: Every sweep action is immutably recorded in `EmailActionLog` with message metadata and execution metrics.

### 🤖 Telegram ChatOps & Interactive Inline Control
- **Interactive Inline Buttons**: Real-time cleanup notifications come with clickable Telegram inline buttons:
  - `[✅ Bật quy tắc ngay]`: Instantly activates newly learned rules with a single tap.
  - `[🗑️ Xóa quy tắc]`: Permanently deletes unwanted rule suggestions directly from Telegram chat without opening a browser.
- **Short 8-Character ID Prefix Matching**: Telegram bot commands support convenient 8-character ID prefixes (e.g., `/enable_rule 67e2a9bf`) instead of cumbersome full Mongo ObjectIds or UUIDs.
- **Comprehensive Command Suite**:
  | Command | Description | Example |
  |---|---|---|
  | `/rules` | Lists all active and inactive cleanup rules with short IDs | `/rules` |
  | `/enable_rule <id>` | Enables a rule by full ID or 8-char prefix | `/enable_rule a1b2c3d4` |
  | `/disable_rule <id>` | Disables a rule by full ID or 8-char prefix | `/disable_rule a1b2c3d4` |
  | `/delete_rule <id>` | Permanently removes a cleanup rule from database | `/delete_rule a1b2c3d4` |
  | `/status` | Reports system health, AI token quota, and job statuses | `/status` |
  | `/readmore <logId>` | Retrieves full execution details for an email action log | `/readmore 67...` |

### 🛡️ Drive Security Guard & Deep Folder Explorer (UC05 & UC06)
- **Folder Change Audit**: Real-time change tracking across monitored Google Drive folders.
- **Hierarchical Folder Explorer**: Deep drill-down navigation into subfolders with breadcrumb trails (`Monitored Folder > Subfolder A > Subfolder B`), total item counts, file sizes, and last modified timestamps.
- **Permission Drift Monitor**: Detects over-shared public files and allows instant 1-click permission revocation.
- **Quarantine Engine**: Isolates suspicious file uploads (`.exe`, `.bat`, `.scr`, `.zip`) into an isolated quarantine folder to prevent malware propagation in shared team drives.

### 📋 Multi-List Tasks Board (Google Tasks)
- **Kanban-Style Multi-List Board**: Monitor multiple Google Task lists side-by-side simultaneously.
- **Multi-Selection Filter**: Select or deselect any combination of task lists dynamically with instant column layout rendering.
- **Real-Time Task Operations**: Toggle task completion status, view overdue and due date badges, and create new tasks directly within each list column.

### 🔄 Google Workspace API Resilience & Auto-Refresh
- **Pre-Flight Token Lifetime Validation**: `EnsureFreshTokenAsync` checks Google OAuth access token expiration before each API call. If less than 5 minutes remain, it transparently refreshes the token using AES-256 GCM encrypted refresh tokens stored in MongoDB.
- **401 Unauthorized Auto-Recovery Retry**: `ExecuteWithRetryAsync` wraps Drive and Sheets operations. If an unexpected `401 Unauthorized` or token revocation occurs mid-operation, the service automatically evicts the stale token, executes a fresh token exchange, and retries the request seamlessly.

### 💳 Financial Telemetry & Read-Only Audit (UC04)
- **11-Field Banking AI Extraction**: Parses bank notification emails via Gemini AI (*Transaction Code, Date/Time, Bank, Credit/Debit, Amount, Fee, Source Account, Target Account, Beneficiary, Category, Description*).
- **Read-Only Protection**: Financial emails are marked as read (`MarkAsReadAsync`), strictly preventing deletion.
- **Monthly Google Sheets Auto-Export**: Automatically generates and organizes monthly spreadsheets (`BaoCaoTaiChinh_{yyyy_MM}`) into user-specified Drive folders with dynamic tab title resolution (`'Trang tính1'`, `'Sheet1'`).

### 📊 Monthly AI Token Quota Tracker (250k Limit)
- **Granular Token Extraction**: Captures `usageMetadata` (prompt, candidate, total tokens) from every Gemini API call.
- **Tiered Threshold Alerts**:
  - **200,000 tokens**: Dispatches a yellow warning via Telegram.
  - **250,000 tokens**: Automatically locks background AI tasks until the 1st of next month (Regex cleanup continues uninterrupted).
- **Interactive Manual AI Calls**: User-initiated drafting in `ComposeEmailModal` remains available with helpful toast reminders of remaining tokens.
- **Visual Analytics**: Interactive progress bar, 200k marker, and breakdown by feature (`EmailReply`, `BankTelemetry`, `EmailCleanup`, `ScheduleExtractor`) in Settings and on the Dashboard.

### 🔔 Cyber-Cockpit UI & Real-Time Alerting (UC12)
- **Modern Dark UI Design**: High-performance cyber-cockpit theme built with PrimeFlex and custom SCSS with 60fps hardware-accelerated animations.
- **Auto-Dismissing Toast Notifications**: Top-right toast notifications auto-dismiss after **4 seconds** for success/info alerts, while critical errors persist until acknowledged.
- **Multi-Channel Alerts**: Telegram push notifications with HTML formatting and Discord embedded webhooks categorized by severity (`critical`, `warning`, `info`).
- **Dynamic Runtime Settings**: Updates to Bot Tokens and Chat IDs made in the Settings UI take effect immediately in database configuration (`AppConfiguration`) without server reboot.

### 💓 Anti-Sleep Keep-Alive Heartbeat (MonsterASP Free Tier)
- **Solves IIS 20-minute Idle Timeout**: MonsterASP free tier terminates worker processes after 20 minutes of inactivity.
- **Secure Keep-Alive Endpoint (`GET /api/v1/public/keep-alive`)**:
  - Protected with `X-KeepAlive-Key` header or `?key=...` query token to prevent bot abuse.
  - Immune to rate limits and reports server uptime, memory usage, and Hangfire active servers count.
  - Supports manual remote execution via `?trigger=<job-id>` or `?trigger=all`.
- **Pre-built GitHub Actions Workflow**: `.github/workflows/keep-alive.yml` automatically pings every 14 minutes, keeping the backend awake 24/7 at **0đ cost**. Also compatible with Cron-job.org and UptimeRobot.

### 📅 Smart Scheduling & Guest Calendar Grid (UC03)
- **Interactive Public Guest View**: Google Calendar-inspired interactive calendar (Week Grid, Month Grid, Agenda view, date navigation) accessible anonymously.
- **Date Range Synchronization**: Query parameters (`startDate`, `endDate`) allow viewing past and future schedules dynamically.
- **Scoped Visibility (`IsPublic`)**: Toggle events and synced tasks between Public and Private visibility to protect sensitive personal schedules.

---

## 🏗 System Architecture

```
┌──────────────────────────────────────────────────────────┐
│              CLIENT (Vue 3 SPA on Vercel — HTTPS)        │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐    │
│  │Dashboard │ │Email Ops │ │Finance   │ │Public    │    │
│  │  View    │ │  View    │ │  View    │ │Calendar  │    │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘    │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐    │
│  │ Tasks    │ │DriveGuard│ │Settings  │ │Toast     │    │
│  │  Board   │ │ Explorer │ │  Center  │ │ Alerts   │    │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘    │
│                    │ Axios + SignalR                      │
│           vercel.json rewrites /api/* →                   │
└────────────────────┼─────────────────────────────────────┘
                     │ Proxy (HTTPS → HTTP)
┌────────────────────┼─────────────────────────────────────┐
│        BACKEND (.NET 8 on MonsterASP — HTTP)             │
│                    │                                      │
│  ┌─────────────────┴───────────────────────────────┐     │
│  │  API Layer: Controllers + SignalR Hub            │     │
│  │  Auth: Google OAuth 2.0 → JWT                    │     │
│  └─────────────────┬───────────────────────────────┘     │
│                    │                                      │
│  ┌─────────────────┴───────────────────────────────┐     │
│  │  Application Layer: Native CQRS Dispatcher      │     │
│  │  Hangfire Recurring Engine (4 Automation Jobs)  │     │
│  └─────────────────┬───────────────────────────────┘     │
│                    │                                      │
│  ┌─────────────────┴───────────────────────────────┐     │
│  │  Infrastructure: Token Resilience & Retry       │     │
│  └──────┬──────────────┬──────────────┬────────────┘     │
│         │              │              │                   │
│  ┌──────┴─────┐ ┌──────┴─────┐ ┌──────┴──────────┐       │
│  │ MongoDB    │ │ Google APIs│ │ Gemini AI       │       │
│  │ Atlas M0   │ │(Gmail,Cal, │ │ (Parse, Learn,  │       │
│  │ (AES-256)  │ │ Drive,Task)│ │  Drafts, Quota) │       │
│  └────────────┘ └────────────┘ └─────────────────┘       │
│         ▲                                                 │
│         └────── Telegram Bot (Inline Buttons & ChatOps)   │
└──────────────────────────────────────────────────────────┘
```

### ⚡ Technical Highlights
- **Clean Architecture**: Decoupled Domain, Application, Infrastructure, and API projects following SOLID principles.
- **Native CQRS Dispatcher**: Custom lightweight generic dispatcher (`IDispatcher`) using .NET 8 native dependency injection—eliminating third-party MediatR dependencies.
- **AES-256 GCM Token Security**: Google OAuth access and refresh tokens are encrypted at rest in MongoDB.
- **OAuth Resilience**: Pre-flight expiration verification (`EnsureFreshTokenAsync`) and automatic 401 retry loops (`ExecuteWithRetryAsync`).
- **Edge HTTPS Proxying**: `vercel.json` rewrites proxy frontend requests seamless across HTTPS (Vercel) to HTTP (MonsterASP) backends, eliminating CORS and mixed-content issues.

---

## 🛠 Tech Stack

| Layer | Technology | Description |
|---|---|---|
| **Backend Framework** | .NET 8 (ASP.NET Core Web API) | C# 12, Clean Architecture, Native Dependency Injection |
| **Background Engine** | Hangfire with MongoStorage | Scheduled recurring automation (Email, Bank, Drive, Calendar) |
| **Frontend Framework** | Vue 3 (Composition API) | TypeScript, Vite 5, SCSS, PrimeFlex, Pinia, Axios |
| **Database** | MongoDB Atlas | M0 Free Tier (NoSQL document store, encrypted tokens) |
| **AI Integration** | Google Gemini API (`GeminiAIService`) | Email parsing, spam rule learning, draft creation, 250k token quota |
| **Google Workspace APIs** | Gmail, Calendar, Drive, Sheets, Tasks | OAuth2 Token Auto-Refresh, 401 Retry, Push Watch Notifications |
| **Alerting & ChatOps** | Telegram Bot, Discord, SignalR | Interactive inline buttons, short ID commands, real-time toasts |
| **Hosting & Anti-Sleep** | MonsterASP.NET & Vercel | IIS Keep-Alive heartbeat, Vercel Serverless Rewrites |

---

## 📂 Project Structure

```
Google-Workspace-Personal-Assistant/
├── .github/workflows/keep-alive.yml         # 💓 GitHub Actions 14-min Anti-Sleep Cron
├── docs/                                    # 📋 System Specifications & Docs
│   ├── 01-PROJECT_OVERVIEW.md               # Vision & Tech Stack
│   ├── 02-USE_CASES.md                      # Detailed Use Cases Specs
│   ├── 03-AUTH_AND_AUTHORIZATION.md         # OAuth2 & JWT Security
│   ├── 04-PROJECT_STRUCTURE.md              # Codebase Architecture Breakdown
│   ├── 05-DATABASE_DESIGN.md                # MongoDB Schemas & Collections
│   ├── 06-API_SPECIFICATION.md              # REST API Endpoints
│   ├── 07-FRONTEND_DESIGN.md                # UI Design System & Mockups
│   ├── 08-DEPLOYMENT_GUIDE.md               # CI/CD & Hosting Setup
│   ├── 09-ADDITIONAL_FEATURES.md            # Extensibility & Future Roadmap
│   ├── PORTFOLIO_BLOG_TEMPLATES.md          # Technical Case Studies & Dev Logs
│   └── agents/                              # 🤖 Autonomous Agent Guidelines & Workflows
│
├── src/
│   ├── backend/                             # 🔧 .NET 8 Backend Solution
│   │   ├── GOpsHub.Domain/                  # Core Entities, Enums & Interfaces
│   │   ├── GOpsHub.Application/             # Native CQRS Commands, Queries & Services
│   │   ├── GOpsHub.Infrastructure/          # Google APIs, AI, Mongo Repositories, Telegram ChatOps
│   │   ├── GOpsHub.API/                     # Web API Controllers & SignalR Hubs
│   │   └── GOpsHub.Tests/                   # 120 Unit & Integration Tests (100% pass)
│   │
│   └── frontend/                            # 🎨 Vue 3 SPA Solution
│       ├── src/
│       │   ├── components/                  # Cyber-Cockpit UI, Bento Grid, Toast Notification
│       │   ├── views/                       # Dashboard, EmailOps, Tasks, DriveGuard, Finance, Calendar, Settings
│       │   ├── services/                    # Axios API Client & SignalR Hub Listener
│       │   └── stores/                      # Pinia State Management
│       └── vite.config.ts
│
├── .env.example                             # 🔒 Sanitized Environment Placeholders
└── vercel.json                              # Vercel Edge Rewrites Proxy Config
```

---

## 💻 Local Quick Start

### 1. Prerequisites
- **.NET 8 SDK** installed
- **Node.js 18+** & **npm** installed
- **MongoDB Atlas** connection string
- **Google Cloud Console App** (OAuth 2.0 Credentials with Gmail, Calendar, Drive, Sheets, Tasks APIs enabled)

### 2. Backend Setup (.NET 8)

```bash
# Navigate to root directory
cp .env.example .env
# Edit .env with your Google OAuth, MongoDB, Gemini API Key, and Telegram Bot credentials

# Navigate to backend directory
cd src/backend

# Build and run the API
dotnet run --project GOpsHub.API
```
> Swagger documentation will be available at `http://localhost:5000/swagger`.  
> Hangfire dashboard is available at `http://localhost:5000/hangfire`.

### 3. Frontend Setup (Vue 3)

```bash
# Navigate to frontend directory
cd src/frontend

# Install dependencies
npm install

# Start Vite development server
npm run dev
```
> Application will be available at `http://localhost:5173`.

### 4. Running Verification Tests

```bash
# Run all backend unit tests (120 tests)
dotnet test src/backend/GOpsHub.sln

# Run frontend build and typecheck
cd src/frontend && npm run build
```

---

## 📄 Documentation

For deep technical insights, database schemas, and architectural logs, explore the [`docs/`](./docs) directory:
- 📖 [Project Overview](./docs/01-PROJECT_OVERVIEW.md)
- ⚙️ [Use Cases Specification](./docs/02-USE_CASES.md)
- 🗄️ [Database Design](./docs/05-DATABASE_DESIGN.md)
- 🌐 [API Specification](./docs/06-API_SPECIFICATION.md)
- 🤖 [Agent Pull Request & Merge Workflow](./docs/agents/pr-merge-workflow.md)
- 📝 [Portfolio Case Study & Technical Dev Logs](./docs/PORTFOLIO_BLOG_TEMPLATES.md)

---

## 🌐 Case Study & Architecture Article

- 📌 **Full Project Case Study**: [G-Ops Hub — Google Workspace Personal Assistant](https://thienhn0910.vercel.app/projects/google-workspace-personal-assistant)
- 📖 **Architecture Deep-Dive Article**: [Architecting G-Ops Hub: .NET CQRS, Hangfire, and Multi-Channel Automation](https://thienhn0910.vercel.app/blog/architecting-g-ops-hub-dotnet-cqrs-hangfire)
- 👨‍💻 **Author Portfolio**: [ThienHN (thienhn0910.vercel.app)](https://thienhn0910.vercel.app/)
- 🚀 **Explore More Engineering Projects**: [Portfolio Projects Showcase](https://thienhn0910.vercel.app/projects)

---

## 📜 License

This project is licensed under the [MIT License](LICENSE).
