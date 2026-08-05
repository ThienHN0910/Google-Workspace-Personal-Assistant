# ⚡ G-Ops Hub — Google Workspace Operations & AI Personal Assistant

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Vue 3](https://img.shields.io/badge/Vue.js-3.x-4FC08D?logo=vuedotjs&logoColor=white)](https://vuejs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178C6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![MongoDB Atlas](https://img.shields.io/badge/MongoDB-Atlas_M0-47A248?logo=mongodb&logoColor=white)](https://www.mongodb.com/)
[![Google Gemini AI](https://img.shields.io/badge/AI-Google_Gemini-8E75B2?logo=google&logoColor=white)](https://ai.google.dev/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

> **G-Ops Hub** is an intelligent, AI-powered personal operations engine built for the Google Workspace ecosystem (Gmail, Calendar, Drive, Sheets, Tasks). It automates repetitive operational workflows, enforces Drive asset security, logs financial transactions, and provides interactive scheduling dashboards.

---

## 🌟 Key Features

### 💳 Financial Telemetry & Drive Auto-Export (UC04)
- **11-Field Banking AI Extraction**: Parses bank emails (VPBank, VCB, TCB, MB Bank, Momo, etc.) via Gemini AI to extract *Transaction Code, Date/Time, Bank, Type (Credit/Debit), Amount, Fee Amount, Source Account, Target Account, Beneficiary Name, Category, and Description*.
- **Monthly Google Sheets Auto-Export**: Automatically creates monthly spreadsheets (`BaoCaoTaiChinh_{yyyy_MM}`) on Google Drive and moves them into a user-configured Drive Folder.
- **Multi-Language Tab Resolution Engine**: Dynamically resolves Google Sheet tab titles (`'Trang tính1'`, `'Sheet1'`) and appends rows cleanly below headers using `INSERT_ROWS`.
- **In-App Drive Configuration**: Dedicated UI panel to configure Drive Folder ID, File Name Pattern, or fixed Spreadsheet ID overrides.

### 📅 Smart Scheduling & Guest Calendar Grid
- **Interactive Public Guest View**: Google Calendar-inspired interactive calendar (Week Grid, Month Grid, Agenda view, date navigation) accessible anonymously.
- **Date Range Synchronization**: Query parameters (`startDate`, `endDate`) allow viewing past and future schedules dynamically.
- **Scoped Visibility (`IsPublic`)**: Toggle events and synced tasks between Public and Private visibility to protect sensitive personal schedules.

### 🛡️ Drive Security Guard (UC05 & UC06)
- **Folder Change Audit**: Real-time change tracking across monitored Google Drive folders.
- **Permission Drift Monitor**: Detects over-shared public files and allows instant 1-click permission revocation.
- **Quarantine Engine**: Isolates suspicious file uploads to prevent malware propagation in shared team drives.

### ✉️ Email Operations & Human-in-the-Loop AI Drafts (UC01 & UC02)
- **Auto-Clean Inbox Zero**: Category-based cleanup rules (Promotions, Social, System Notifications) to keep inbox lean.
- **Human-in-the-Loop AI Replies**: AI drafts responses for actionable emails, allowing Admin review, editing, or approval before sending.

---

## 🏗 System Architecture

```
┌──────────────────────────────────────────────────────────┐
│              CLIENT (Vue 3 SPA on Vercel — HTTPS)        │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐    │
│  │Dashboard │ │Email Ops │ │Finance   │ │Public    │    │
│  │  View    │ │  View    │ │  View    │ │Calendar  │    │
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
│  └─────────────────┬───────────────────────────────┘     │
│                    │                                      │
│  ┌────────────┐ ┌──┴──────────┐ ┌─────────────────┐     │
│  │ MongoDB    │ │ Google APIs │ │ Gemini AI       │     │
│  │ Atlas M0   │ │ (Gmail,Cal, │ │ (Draft, Parse)  │     │
│  │ (Free)     │ │  Drive,etc) │ │                 │     │
│  └────────────┘ └─────────────┘ └─────────────────┘     │
└──────────────────────────────────────────────────────────┘
```

### ⚡ Technical Highlights
- **Clean Architecture**: Decoupled Domain, Application, Infrastructure, and API projects following SOLID principles.
- **Native CQRS Dispatcher**: Custom lightweight generic dispatcher (`IDispatcher`) using .NET 8 native dependency injection—eliminating third-party MediatR dependencies.
- **AES-256 GCM Token Security**: Google OAuth access and refresh tokens are encrypted at rest in MongoDB.
- **Edge HTTPS Proxying**: `vercel.json` rewrites proxy frontend requests seamless across HTTPS (Vercel) to HTTP (MonsterASP) backends, eliminating CORS and mixed-content issues.

---

## 🛠 Tech Stack

| Layer | Technology | Description |
|---|---|---|
| **Backend Framework** | .NET 8 (ASP.NET Core Web API) | C# 12, Clean Architecture, Native Dependency Injection |
| **Frontend Framework** | Vue 3 (Composition API) | TypeScript, Vite 5, SCSS, PrimeFlex, Pinia, Axios |
| **Database** | MongoDB Atlas | M0 Free Tier (NoSQL document store) |
| **AI Integration** | Google Gemini API (`GeminiAIService`) | Email parsing, schedule extraction, draft creation |
| **Google Workspace APIs** | Gmail, Calendar, Drive, Sheets, Tasks | OAuth2 Token Auto-Refresh, Push Watch Notifications |
| **Real-time Engine** | ASP.NET Core SignalR | Live dashboard alerts and telemetry updates |
| **Hosting** | MonsterASP.NET & Vercel | Vercel Serverless Rewrites for HTTP/HTTPS proxying |

---

## 📂 Project Structure

```
Google-Workspace-Personal-Assistant/
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
│   └── PORTFOLIO_BLOG_TEMPLATES.md          # Technical Case Studies & Dev Logs
│
├── src/
│   ├── backend/                             # 🔧 .NET 8 Backend Solution
│   │   ├── GOpsHub.Domain/                  # Core Entities, Enums & Interfaces
│   │   ├── GOpsHub.Application/             # Native CQRS Commands, Queries & Services
│   │   ├── GOpsHub.Infrastructure/          # Google APIs, AI, Mongo Repositories
│   │   ├── GOpsHub.API/                     # Web API Controllers & SignalR Hubs
│   │   └── GOpsHub.Tests/                   # Unit & Integration Tests
│   │
│   └── frontend/                            # 🎨 Vue 3 SPA Solution
│       ├── src/
│       │   ├── components/                  # Reusable UI & Layout Components
│       │   ├── views/                       # Dashboard, Finance, Calendar, Drive, Email Views
│       │   ├── services/                    # Axios API Client & SignalR Listener
│       │   └── stores/                      # Pinia State Management
│       └── vite.config.ts
│
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
# Navigate to backend directory
cd src/backend

# Configure appsettings.json or Environment Variables
# Key variables: MongoDb__ConnectionString, ADMIN_EMAIL, Google__ClientId, Google__ClientSecret, Gemini__ApiKey

# Build and run the API
dotnet run --project GOpsHub.API
```
> Swagger documentation will be available at `http://localhost:5000/swagger`.

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

---

## 📄 Documentation

For deep technical insights, database schemas, and architectural logs, explore the [`docs/`](./docs) directory:
- 📖 [Project Overview](./docs/01-PROJECT_OVERVIEW.md)
- ⚙️ [Use Cases Specification](./docs/02-USE_CASES.md)
- 🗄️ [Database Design](./docs/05-DATABASE_DESIGN.md)
- 🌐 [API Specification](./docs/06-API_SPECIFICATION.md)
- 📝 [Portfolio Case Study & Technical Dev Logs](./docs/PORTFOLIO_BLOG_TEMPLATES.md)

---

## 📜 License

This project is licensed under the [MIT License](LICENSE).
