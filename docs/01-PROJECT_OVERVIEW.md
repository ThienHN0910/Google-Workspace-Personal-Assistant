# 📋 G-Ops Hub — Project Overview

> **Google Workspace Operations Hub** — Hệ thống quản trị vận hành tự động toàn diện xoay quanh hệ sinh thái Google Workspace.

---

## 1. Tầm nhìn dự án (Vision)

G-Ops Hub là một **Personal Automation Platform** giúp người dùng:

- **Tự động hóa** các tác vụ lặp lại trên Gmail, Calendar, Drive, Sheets
- **Bảo vệ** dữ liệu và quyền truy cập trên Google Drive
- **Tối ưu** quản lý tài chính cá nhân bằng cách tự động ghi nhận giao dịch
- **Tăng cường** năng suất với AI-powered email drafting & scheduling
- **Giám sát** mọi hoạt động trên Workspace qua Dashboard trực quan

---

## 2. Mô hình người dùng (User Model)

Hệ thống sử dụng mô hình **đơn giản hóa**:

| Role | Mô tả | Quyền |
|------|--------|-------|
| **Admin** | Owner duy nhất (`hnt.vn.vn@gmail.com`) — đăng nhập qua Google OAuth | Full access mọi tính năng |
| **Anonymous** | Bất kỳ ai truy cập mà không login | Chỉ xem thông tin public (lịch bận, trạng thái hệ thống) |

> Không có registration flow — chỉ Admin login bằng Google, email phải khớp chính xác.

---

## 3. Tech Stack

### 3.1 Backend — .NET 8 (ASP.NET Core Web API)

| Layer | Công nghệ | Vai trò |
|-------|-----------|---------|
| Runtime | .NET 8 LTS | Long-term support, hiệu suất cao |
| Web Framework | ASP.NET Core Web API | RESTful API endpoints |
| Database | **MongoDB Atlas (Free Tier M0)** | NoSQL document store — 512MB free |
| ODM | **MongoDB.Driver** | Official .NET MongoDB driver |
| Background Jobs | Hangfire (MongoDB storage) | Scheduled tasks (cron-based) |
| Authentication | Google OAuth 2.0 + JWT | Admin authentication |
| AI Integration | **Google Gemini API** | Email drafting, summarization, extraction |
| Logging | Serilog | Structured logging |
| Validation | FluentValidation | Request validation |
| Mapping | Mapster | DTO mapping |
| API Docs | Swagger / Scalar | API documentation |
| Real-time | SignalR | Live notifications |
| CQRS | **Self-implemented** (no MediatR) | Command/Query separation |
| Testing | xUnit + Moq + FluentAssertions | Unit & Integration tests |

### 3.2 Frontend — Vue 3 + TypeScript

| Layer | Công nghệ | Vai trò |
|-------|-----------|---------|
| Framework | Vue 3 (Composition API) | Reactive UI |
| Language | TypeScript | Type safety |
| Build Tool | Vite 5 | Fast build & HMR |
| State | Pinia | Centralized state |
| Router | Vue Router 4 | SPA routing |
| HTTP | Axios | API communication |
| UI Library | **PrimeVue** | Component library (best DX for Vue 3) |
| Charts | Apache ECharts | Data visualization |
| CSS | SCSS + CSS Variables | Theming |
| Real-time | @microsoft/signalr | Live updates |
| i18n | Vue I18n | VI / EN |
| Testing | Vitest + Vue Test Utils | Testing |

### 3.3 Hosting & Infrastructure

| Service | Provider | Chi tiết |
|---------|----------|----------|
| **Backend API** | **MonsterASP.NET** | .NET hosting (HTTP) |
| **Frontend SPA** | **Vercel** | Static hosting (HTTPS) |
| **Database** | **MongoDB Atlas** | Free Tier M0 (512MB) |
| **Proxy Config** | `vercel.json` | Rewrite FE (HTTPS) → BE (HTTP) để tránh mixed content |
| Version Control | GitHub | Source code + CI/CD |

> ⚠️ **HTTPS/HTTP proxy**: Vercel (HTTPS) gọi MonsterASP (HTTP) cần `vercel.json` rewrites để proxy API requests qua Vercel serverless, tránh lỗi mixed content.

### 3.4 Không sử dụng

| Công nghệ | Lý do bỏ |
|-----------|----------|
| ~~Redis~~ | Dự án cá nhân, không cần cache layer |
| ~~PostgreSQL / SQL Server~~ | Chuyển sang MongoDB Atlas free tier |
| ~~MediatR~~ | Tự implement CQRS pattern |
| ~~Docker (production)~~ | Hosting trên MonsterASP + Vercel, không cần container |

---

## 4. Kiến trúc tổng quan (High-Level Architecture)

```
┌──────────────────────────────────────────────────────────┐
│              CLIENT (Vue 3 SPA on Vercel — HTTPS)        │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐    │
│  │Dashboard │ │Email Ops │ │Finance   │ │Drive     │    │
│  │  View    │ │  View    │ │  View    │ │Guard View│    │
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
│  │  Auth: Google OAuth → JWT                        │     │
│  └─────────────────┬───────────────────────────────┘     │
│                    │                                      │
│  ┌─────────────────┴───────────────────────────────┐     │
│  │  Application Layer: CQRS Commands/Queries       │     │
│  │  + Background Jobs (Hangfire)                   │     │
│  └─────────────────┬───────────────────────────────┘     │
│                    │                                      │
│  ┌────────────┐ ┌──┴──────────┐ ┌─────────────────┐     │
│  │ MongoDB    │ │ Google APIs │ │ Gemini AI       │     │
│  │ Atlas M0   │ │ (Gmail,Cal, │ │ (Draft, Parse)  │     │
│  │ (Free)     │ │  Drive,etc) │ │                 │     │
│  └────────────┘ └─────────────┘ └─────────────────┘     │
└──────────────────────────────────────────────────────────┘
```

---

## 5. Nguyên tắc thiết kế

1. **Clean Architecture** — Domain / Application / Infrastructure / Presentation
2. **SOLID Principles**
3. **CQRS Pattern** (self-implemented) — Tách Command & Query
4. **Repository Pattern** cho MongoDB
5. **Event-Driven** — Domain events
6. **Human-in-the-Loop** — AI luôn cần sự duyệt của Admin

---

## 6. Roadmap & Sprint Assignment

| Phase | Use Cases | AI Agent | Trạng thái |
|-------|-----------|----------|-----------|
| **v0.1 Foundation** | Auth (Google OAuth), Base Infrastructure, MongoDB setup | 🧠 **Opus** | ✅ **Completed** |
| **v0.2 Email Engine** | UC01 (Auto-Clean), UC02 (AI Draft) | 🧠 **Opus** | ✅ **Completed** |
| **v0.3 Smart Scheduling** | UC03 (Schedule Extractor) | ⚡ **Flash** | ✅ **Completed** |
| **v0.4 Finance Tracker** | UC04 (Transaction Logging) | ⚡ **Flash** | ✅ **Completed** |
| **v0.5 Drive Guard** | UC05 (Audit), UC06 (File Guard) | 🧠 **Opus** | ✅ **Completed** |
| **v1.0 MVP Polish** | Testing, bug fixes, deploy | 🧠 **Opus** | ✅ **Completed** |
| **v1.1 Enhanced** | UC07, UC09, UC10, UC12, UC13, UC14, UC16 | ⚡ **Flash** | ✅ **Completed** |
| **v1.2 Full Suite** | UC08, UC11, UC15, UC17, UC18 | ⚡ **Flash** | ✅ **Completed** |

### 📋 Agent Handoff Guide

Khi chuyển sprint giữa Opus ↔ Flash, cần note:

1. **Đọc docs/** trước khi code — đặc biệt `04-PROJECT_STRUCTURE.md` và `06-API_SPECIFICATION.md`
2. **Kiểm tra existing patterns** — xem code đã implement ở sprint trước
3. **Không tự ý thay đổi architecture** — follow Clean Architecture đã định
4. **Chạy tests** trước khi commit: `dotnet test` + `npm run test`
5. **Update docs** nếu có thay đổi API/Schema
