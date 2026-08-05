# 📝 G-Ops Hub — Portfolio Case Study, Dev Log & Technical Project Overview

This document provides complete, publication-ready portfolio entries (**Executive Case Study**, **Technical Dev Logs**, and **Project Architecture Overview**) for the **G-Ops Hub** (Google Workspace Operations Hub) repository.

---

## 📄 Section 1: Project Case Study — G-Ops Hub

# G-Ops Hub — Intelligent Automation & Security Operations Platform for Google Workspace

## 1. Executive Summary & Core Objectives
- **Context & Problem**: Personal and executive operations across Google Workspace services (Gmail, Calendar, Drive, Sheets) create severe productivity fragmentation. Repetitive email triage, manual financial logging, schedule coordination, and unmonitored Drive permission risks lead to cognitive overload and security oversights.
- **Project Vision**: **G-Ops Hub** is a unified personal operations platform that leverages AI (Google Gemini) and deep Google Workspace API integrations to automate workflow loops, protect Drive assets, and log financial telemetry seamlessly.
- **Core Engineering Goals**:
  - Implement a zero-dependency **Clean Architecture (.NET 8 Web API)** backend utilizing a self-implemented lightweight **CQRS Pattern** (no MediatR overhead).
  - Build an intelligent **Financial Telemetry Engine** that parses bank transaction emails via Gemini AI and automatically exports monthly Google Sheets to configured Google Drive folders.
  - Deliver an interactive, public-facing **Google Calendar Guest View** (Week/Month/Agenda Grid) with strict public/private event visibility scoping (`isPublic`).
  - Build a high-performance **Vue 3 + TypeScript SPA** with dark-mode aesthetic glassmorphism, infinite scrolling stability, and real-time operations telemetry.

---

## 2. Technical Architecture & Tech Stack

| Domain | Component / Technology | Technical Description |
|---|---|---|
| **Backend Core** | .NET 8 ASP.NET Core Web API | Clean Architecture (Domain, Application, Infrastructure, API layers), Native Dependency Injection |
| **CQRS Engine** | Self-Implemented Dispatcher | Custom generic `IDispatcher`, `ICommandHandler`, `IQueryHandler` without third-party MediatR dependencies |
| **Database** | MongoDB Atlas (M0 Free Tier) | Document storage for transactions, rules, audit logs, schedules, and application configurations |
| **Security & Auth** | Google Identity Services OAuth 2.0 + AES-256 GCM | Admin Single-User Auth, JWT bearer tokens, encrypted Google Access/Refresh tokens |
| **AI Integration** | Google Gemini API (`GeminiAIService`) | Single & batch email transaction extraction, priority scoring, smart reply generation |
| **Google APIs** | Gmail, Calendar, Drive, Sheets, Tasks APIs | OAuth2 token auto-refresh, push notifications, batch email processing, folder permission auditing |
| **Frontend SPA** | Vue 3 (Composition API) + TypeScript | Vite 5 build tool, SCSS variables, PrimeFlex layout, Pinia state management, Axios interceptors |
| **Public Guest View** | Interactive Calendar Component | Week Grid, Month Grid, Agenda view, date navigation, date range filter params (`startDate`, `endDate`) |
| **Infrastructure** | MonsterASP.NET (BE HTTP) & Vercel (FE HTTPS) | Serverless proxying via `vercel.json` rewrites to eliminate HTTPS/HTTP mixed-content errors |

---

## 3. Engineering Challenges & Architectural Solutions

### Challenge 1: MediatR Framework Overhead vs Native CQRS Dispatcher
- **The Problem**: Standard CQRS implementations in .NET heavily rely on third-party MediatR packages, introducing assembly scanning startup overhead and external framework coupling for single-admin microservice requirements.
- **The Solution**: Engineered a native CQRS `Dispatcher` using .NET's built-in `IServiceProvider` to dynamically resolve handlers (`ICommandHandler<TCommand, TResult>` and `IQueryHandler<TQuery, TResult>`) with 0ms startup overhead and zero external dependencies.

### Challenge 2: Google Sheets API Range Resolution & Multi-Language Account Localization
- **The Problem**: When creating monthly financial spreadsheets, Google Sheets API creates default sheet tabs with localized names (e.g. `'Trang tính1'` in Vietnamese vs `'Sheet1'` in English). Hardcoding range `"Sheet1!A1"` or passing unanchored `"A1"` caused HTTP 400 Bad Request errors (`Unable to parse range`), resulting in files created with headers but empty data rows.
- **The Solution**: Refactored `SheetsApiService.cs` to query spreadsheet metadata dynamically via `service.Spreadsheets.Get()`, resolving the exact title of the first tab (`'Trang tính1'` / `'Sheet1'`), wrapping ranges as `$'{{actualSheetTitle}}'!A1`, and explicitly enforcing `InsertDataOption.INSERTROWS`.

### Challenge 3: Public Guest Calendar View & Scoped Event Visibility
- **The Problem**: Public users need to check availability without exposing private details or seeing cluttered upcoming lists.
- **The Solution**: Built `PublicCalendarView.vue` with Week/Month/Agenda grid layouts powered by date range query parameters (`startDate`, `endDate`). Added an `IsPublic` flag to manual event creation and task calendar sync (defaulting to `isPublic = true`), ensuring private events remain hidden from public views.

### Challenge 4: Infinite Scroll Jumping & Unmounting
- **The Problem**: Scrolling down finance and calendar lists triggered page 2+ fetches, causing top-level `v-if="loading"` conditions to unmount data tables and reset scroll positions back to the top.
- **The Solution**: Replaced top-level loading condition with `v-if="loading && items.length === 0"` and implemented a 400ms emission lock and min-height in `InfiniteScrollObserver.vue`.

---

## 4. Key Metrics & Business Impact
- **Zero Third-Party CQRS Dependencies**: 100% native .NET 8 implementation.
- **100% Banking Transaction Coverage**: Extracts 11 structured fields per transaction (Transaction Code, Date/Time, Bank, Type, Amount, Fee Amount, Debit Account, Credit Account, Beneficiary Name, Category, Description).
- **Automated Google Drive Monthly Export**: Dynamically creates/relocates monthly spreadsheets (e.g. `BaoCaoTaiChinh_2026_08`) into configured Google Drive folders.

---

## 📄 Section 2: Technical Dev Logs

### [Dev Log 1] Native CQRS Dispatcher in .NET 8 (Zero MediatR Dependency)

```csharp
// Application/Common/CQRS/IDispatcher.cs
public interface ICommand<TResponse> { }
public interface IQuery<TResponse> { }

public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken ct = default);
}

public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken ct = default);
}

public interface IDispatcher
{
    Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);
    Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken ct = default);
}

// Application/Common/CQRS/Dispatcher.cs
public class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
        dynamic handler = _serviceProvider.GetRequiredService(handlerType);
        return await handler.HandleAsync((dynamic)command, ct);
    }

    public async Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
        dynamic handler = _serviceProvider.GetRequiredService(handlerType);
        return await handler.HandleAsync((dynamic)query, ct);
    }
}
```

---

### [Dev Log 2] Multi-Language Google Sheets Tab Resolution & Monthly Drive Export

```csharp
// Infrastructure/GoogleApis/SheetsApiService.cs
public async Task AppendRowAsync(string spreadsheetId, string sheetName, IList<object> values, CancellationToken ct = default)
{
    var service = await GetSheetsClientAsync(ct);
    if (service == null) return;

    try
    {
        // Dynamically resolve actual sheet tab title (e.g. 'Trang tính1' vs 'Sheet1')
        string actualSheetTitle = sheetName;
        if (string.IsNullOrWhiteSpace(actualSheetTitle) || actualSheetTitle.Equals("A1", StringComparison.OrdinalIgnoreCase) || actualSheetTitle.Equals("Sheet1", StringComparison.OrdinalIgnoreCase))
        {
            var meta = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync(ct);
            actualSheetTitle = meta.Sheets?.FirstOrDefault()?.Properties?.Title ?? "Sheet1";
        }

        var valueRange = new ValueRange { Values = new List<IList<object>> { values } };
        var request = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, $"'{actualSheetTitle}'!A1");
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
        request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

        await request.ExecuteAsync(ct);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error appending row to Google Spreadsheet {SpreadsheetId}", spreadsheetId);
        throw;
    }
}
```

---

## 📄 Section 3: Project Overview

# G-Ops Hub (Google Workspace Operations Hub)

> An intelligent, AI-powered personal operations assistant that automates Gmail triage, Calendar scheduling, financial transaction telemetry logging, and Drive security audits.

### ✨ Key Features Matrix
- **Banking Email Telemetry (UC04)**: Extracts 11 structured financial fields via Gemini AI, displaying transactions in an interactive table and auto-exporting monthly Google Sheets to designated Drive folders.
- **Interactive Guest Calendar**: Public Google Calendar interface (Week, Month, Agenda grids) supporting date range filtering and public/private event visibility scoping.
- **Drive Security Guard (UC05 & UC06)**: Monitored folders, permissions drift auditing, public sharing revocation, and quarantine file management.
- **Human-in-the-Loop AI Mail Reply (UC02)**: AI draft generation for inbox emails requiring action, with Admin approval workflows before sending.
- **Auto-Clean Inbox (UC01)**: Category-based rules (Promotions, Social, System) for periodic email cleanup.

### 🔗 Architecture Diagram
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
│  │  Auth: Google OAuth → JWT                        │     │
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
