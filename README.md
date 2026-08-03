# ⚡ G-Ops Hub (Google Workspace Operations Assistant)

> Hệ thống tự động hóa vận hành cá nhân dành cho Google Workspace (Email, Calendar, Finance & Drive Security).

---

## 🛠 Tech Stack

- **Backend**: .NET 8 (ASP.NET Core Web API), Clean Architecture, Self-implemented CQRS, SignalR.
- **Frontend**: Vue 3, TypeScript, PrimeVue 4 (Aura Theme), Pinia, Vite.
- **Database**: MongoDB Atlas (Free Tier M0).
- **AI**: Google Gemini API (Primary Provider).
- **Hosting**: MonsterASP.NET (Backend - HTTP) + Vercel (Frontend - HTTPS) with `vercel.json` rewrite proxy.

---

## 🚀 Cấu trúc dự án

```
Google-Workspace-Personal-Assistant/
├── docs/                 # Tài liệu hệ thống (01-09)
├── src/
│   ├── backend/          # .NET 8 Solution (Clean Architecture)
│   │   ├── GOpsHub.Domain
│   │   ├── GOpsHub.Application
│   │   ├── GOpsHub.Infrastructure
│   │   ├── GOpsHub.API
│   │   └── GOpsHub.Tests
│   └── frontend/         # Vue 3 + PrimeVue SPA
└── vercel.json           # Vercel proxy configuration
```

---

## 💻 Chạy Local

### 1. Backend (.NET 8)

```bash
cd src/backend
dotnet run --project GOpsHub.API
# Open Swagger: http://localhost:5000/swagger
```

### 2. Frontend (Vue 3)

```bash
cd src/frontend
npm install
npm run dev
# Open Web: http://localhost:5173
```
