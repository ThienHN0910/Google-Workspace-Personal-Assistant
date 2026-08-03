# 🚀 G-Ops Hub — Deployment Guide

> Hướng dẫn triển khai: MonsterASP.NET (Backend) + Vercel (Frontend) + MongoDB Atlas (Database).

---

## 1. Prerequisites

### 1.1 Development

| Tool | Version | Mục đích |
|------|---------|----------|
| .NET SDK | 8.0+ | Backend development |
| Node.js | 20 LTS | Frontend development |
| Git | 2.40+ | Version control |

### 1.2 Accounts cần tạo

| Service | URL | Tier |
|---------|-----|------|
| **MongoDB Atlas** | [mongodb.com/atlas](https://www.mongodb.com/atlas) | Free M0 (512MB) |
| **Google Cloud Console** | [console.cloud.google.com](https://console.cloud.google.com) | Free (OAuth + API) |
| **Google AI Studio** | [aistudio.google.com](https://aistudio.google.com) | Free (Gemini API) |
| **MonsterASP.NET** | [monsterasp.net](https://monsterasp.net) | Paid (.NET hosting) |
| **Vercel** | [vercel.com](https://vercel.com) | Free (Hobby) |
| **GitHub** | [github.com](https://github.com) | Free |

---

## 2. Google Cloud Setup

1. Tạo project tại Google Cloud Console
2. Bật APIs: Gmail, Calendar, Drive, Sheets, Docs
3. Tạo OAuth 2.0 Credentials:
   - Type: Web application
   - Authorized redirect URIs:
     - `http://localhost:5000/api/v1/auth/google-callback` (dev)
     - `http://your-app.monsterasp.net/api/v1/auth/google-callback` (prod)
4. Cấu hình OAuth Consent Screen
5. Lấy Gemini API Key từ AI Studio

---

## 3. MongoDB Atlas Setup

1. Tạo cluster M0 (Free Tier)
2. Tạo database user: `gopshub_user`
3. Whitelist IP:
   - `0.0.0.0/0` (allow all — cho MonsterASP dynamic IP)
4. Lấy connection string:
   ```
   mongodb+srv://gopshub_user:<password>@cluster0.xxxxx.mongodb.net/gopshub?retryWrites=true&w=majority
   ```

---

## 4. Environment Variables

### `.env.example`

```env
# MongoDB Atlas
MONGODB_CONNECTION_STRING=mongodb+srv://user:pass@cluster0.xxx.mongodb.net/gopshub
MONGODB_DATABASE_NAME=gopshub

# JWT
JWT_SECRET=your-256-bit-secret-key-minimum-32-characters
JWT_ISSUER=gopshub
JWT_AUDIENCE=gopshub-client

# Google OAuth
GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your-client-secret
GOOGLE_REDIRECT_URI=http://localhost:5000/api/v1/auth/google-callback

# Admin
ADMIN_EMAIL=hnt.vn.vn@gmail.com

# AI
GEMINI_API_KEY=your-gemini-api-key

# Frontend URL (for CORS)
FRONTEND_URL=http://localhost:5173

# Discord Webhook (optional)
DISCORD_WEBHOOK_URL=

# Token Encryption
TOKEN_ENCRYPTION_KEY=your-32-char-aes-key-here
```

---

## 5. Local Development

### 5.1 Backend

```bash
cd src/backend
dotnet restore
dotnet run --project GOpsHub.API
# → http://localhost:5000
# → Swagger: http://localhost:5000/swagger
```

### 5.2 Frontend

```bash
cd src/frontend
npm install
npm run dev
# → http://localhost:5173
```

### 5.3 Vite Dev Proxy (local development)

```typescript
// vite.config.ts
export default defineConfig({
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/hubs': {
        target: 'http://localhost:5000',
        ws: true,
      }
    }
  }
});
```

---

## 6. Production Deployment

### 6.1 Backend → MonsterASP.NET

1. Publish .NET project:
   ```bash
   cd src/backend
   dotnet publish GOpsHub.API -c Release -o ./publish
   ```
2. Upload `publish/` folder lên MonsterASP qua FTP/Panel
3. Cấu hình environment variables trong MonsterASP control panel
4. Đảm bảo .NET 8 runtime available

### 6.2 Frontend → Vercel

1. Connect GitHub repo với Vercel
2. Settings:
   - **Framework Preset**: Vue.js
   - **Build Command**: `cd src/frontend && npm run build`
   - **Output Directory**: `src/frontend/dist`
   - **Root Directory**: `.` (repo root)
3. Add environment variables trong Vercel dashboard

### 6.3 Vercel Proxy Configuration

```jsonc
// src/frontend/vercel.json
{
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "http://your-app.monsterasp.net/api/:path*"
    },
    {
      "source": "/hubs/:path*",
      "destination": "http://your-app.monsterasp.net/hubs/:path*"
    }
  ]
}
```

> ⚠️ **Quan trọng**: Vercel rewrites proxy HTTPS → HTTP requests, giải quyết mixed content. Browser thấy cùng domain (Vercel) nên không bị block.

### 6.4 CORS Configuration (.NET)

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",           // Dev
            "https://your-app.vercel.app"      // Production
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});
```

---

## 7. CI/CD (GitHub Actions)

```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore
        working-directory: src/backend
      - run: dotnet build --no-restore
        working-directory: src/backend
      - run: dotnet test --no-build
        working-directory: src/backend

  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '20'
      - run: npm ci
        working-directory: src/frontend
      - run: npm run build
        working-directory: src/frontend
      - run: npm run test -- --run
        working-directory: src/frontend
```

---

## 8. Production Checklist

- [ ] MongoDB Atlas cluster created (M0 Free)
- [ ] Google Cloud OAuth configured
- [ ] Gemini API key obtained
- [ ] MonsterASP deployment working
- [ ] Vercel connected to GitHub
- [ ] `vercel.json` proxy rewrites working
- [ ] CORS configured for Vercel domain
- [ ] JWT secret set (≥ 32 characters)
- [ ] Token encryption key set
- [ ] Admin email verified (`hnt.vn.vn@gmail.com`)
- [ ] Hangfire dashboard secured
- [ ] Serilog logging configured
- [ ] Health check endpoint active
