# 📁 G-Ops Hub — Project Structure

> Cấu trúc thư mục dự án theo Clean Architecture (.NET 8 + Vue 3 + MongoDB).

---

## Monorepo Structure

```
Google-Workspace-Personal-Assistant/
│
├── docs/                                    # 📋 Tài liệu dự án
│   ├── 01-PROJECT_OVERVIEW.md
│   ├── 02-USE_CASES.md
│   ├── 03-AUTH_AND_AUTHORIZATION.md
│   ├── 04-PROJECT_STRUCTURE.md              # (file này)
│   ├── 05-DATABASE_DESIGN.md
│   ├── 06-API_SPECIFICATION.md
│   ├── 07-FRONTEND_DESIGN.md
│   ├── 08-DEPLOYMENT_GUIDE.md
│   └── 09-ADDITIONAL_FEATURES.md
│
├── src/
│   ├── backend/                             # 🔧 .NET 8 Backend
│   │   ├── GOpsHub.sln                      # Solution file
│   │   │
│   │   ├── GOpsHub.Domain/                  # 🏛️ Domain Layer (zero dependencies)
│   │   │   ├── Entities/
│   │   │   │   ├── AdminUser.cs
│   │   │   │   ├── CleanupRule.cs
│   │   │   │   ├── CleanupLog.cs
│   │   │   │   ├── AIDraft.cs
│   │   │   │   ├── ReplyTemplate.cs
│   │   │   │   ├── ExtractedSchedule.cs
│   │   │   │   ├── Transaction.cs
│   │   │   │   ├── MonitoredFolder.cs
│   │   │   │   ├── DriveAuditLog.cs
│   │   │   │   ├── SecurityAlert.cs
│   │   │   │   ├── Notification.cs
│   │   │   │   └── BackupRecord.cs
│   │   │   ├── Enums/
│   │   │   │   ├── CleanupAction.cs
│   │   │   │   ├── DraftStatus.cs
│   │   │   │   ├── ScheduleStatus.cs
│   │   │   │   ├── TransactionType.cs
│   │   │   │   ├── DriveAction.cs
│   │   │   │   ├── AlertSeverity.cs
│   │   │   │   ├── AlertType.cs
│   │   │   │   └── BackupStatus.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── Money.cs
│   │   │   │   └── EmailInfo.cs
│   │   │   ├── Events/
│   │   │   │   ├── IDomainEvent.cs
│   │   │   │   ├── EmailCleanedEvent.cs
│   │   │   │   ├── DraftCreatedEvent.cs
│   │   │   │   ├── TransactionLoggedEvent.cs
│   │   │   │   └── SecurityAlertEvent.cs
│   │   │   ├── Interfaces/
│   │   │   │   ├── IRepository.cs
│   │   │   │   └── IDomainEventDispatcher.cs
│   │   │   └── Common/
│   │   │       ├── BaseEntity.cs
│   │   │       └── Result.cs
│   │   │
│   │   ├── GOpsHub.Application/             # 📋 Application Layer
│   │   │   ├── Common/
│   │   │   │   ├── CQRS/                   # Self-implemented CQRS
│   │   │   │   │   ├── ICommand.cs
│   │   │   │   │   ├── ICommandHandler.cs
│   │   │   │   │   ├── IQuery.cs
│   │   │   │   │   ├── IQueryHandler.cs
│   │   │   │   │   └── Dispatcher.cs
│   │   │   │   ├── Interfaces/
│   │   │   │   │   ├── IGmailService.cs
│   │   │   │   │   ├── ICalendarService.cs
│   │   │   │   │   ├── IDriveService.cs
│   │   │   │   │   ├── ISheetsService.cs
│   │   │   │   │   ├── IAIService.cs
│   │   │   │   │   └── INotificationService.cs
│   │   │   │   ├── Models/
│   │   │   │   │   ├── PagedResult.cs
│   │   │   │   │   └── ApiResponse.cs
│   │   │   │   └── Mappings/
│   │   │   │       └── MappingConfig.cs
│   │   │   │
│   │   │   ├── Features/
│   │   │   │   ├── Auth/
│   │   │   │   │   ├── Commands/GoogleLoginCommand.cs
│   │   │   │   │   └── Queries/GetCurrentUserQuery.cs
│   │   │   │   │
│   │   │   │   ├── EmailOps/
│   │   │   │   │   ├── Commands/
│   │   │   │   │   │   ├── RunCleanupCommand.cs
│   │   │   │   │   │   ├── CreateCleanupRuleCommand.cs
│   │   │   │   │   │   ├── ApproveDraftCommand.cs
│   │   │   │   │   │   └── RejectDraftCommand.cs
│   │   │   │   │   └── Queries/
│   │   │   │   │       ├── GetCleanupRulesQuery.cs
│   │   │   │   │       ├── GetPendingDraftsQuery.cs
│   │   │   │   │       └── GetCleanupLogsQuery.cs
│   │   │   │   │
│   │   │   │   ├── Scheduling/
│   │   │   │   │   ├── Commands/ConfirmScheduleCommand.cs
│   │   │   │   │   └── Queries/GetExtractedSchedulesQuery.cs
│   │   │   │   │
│   │   │   │   ├── Finance/
│   │   │   │   │   ├── Commands/CategorizeTransactionCommand.cs
│   │   │   │   │   └── Queries/
│   │   │   │   │       ├── GetTransactionsQuery.cs
│   │   │   │   │       └── GetMonthlySummaryQuery.cs
│   │   │   │   │
│   │   │   │   ├── DriveGuard/
│   │   │   │   │   ├── Commands/
│   │   │   │   │   │   ├── AddMonitoredFolderCommand.cs
│   │   │   │   │   │   ├── QuarantineFileCommand.cs
│   │   │   │   │   │   └── RevokePermissionCommand.cs
│   │   │   │   │   └── Queries/
│   │   │   │   │       ├── GetAuditLogsQuery.cs
│   │   │   │   │       └── GetSecurityAlertsQuery.cs
│   │   │   │   │
│   │   │   │   └── Dashboard/
│   │   │   │       └── Queries/
│   │   │   │           ├── GetDashboardSummaryQuery.cs
│   │   │   │           └── GetActivityFeedQuery.cs
│   │   │   │
│   │   │   └── DependencyInjection.cs
│   │   │
│   │   ├── GOpsHub.Infrastructure/          # 🔌 Infrastructure Layer
│   │   │   ├── Persistence/
│   │   │   │   ├── MongoDbContext.cs
│   │   │   │   ├── MongoDbSettings.cs
│   │   │   │   ├── Repositories/
│   │   │   │   │   └── MongoRepository.cs
│   │   │   │   └── Seed/
│   │   │   │       └── DataSeeder.cs
│   │   │   │
│   │   │   ├── GoogleApis/
│   │   │   │   ├── GoogleAuthService.cs
│   │   │   │   ├── GmailApiService.cs
│   │   │   │   ├── CalendarApiService.cs
│   │   │   │   ├── DriveApiService.cs
│   │   │   │   ├── SheetsApiService.cs
│   │   │   │   └── GoogleTokenManager.cs
│   │   │   │
│   │   │   ├── AI/
│   │   │   │   ├── GeminiAIService.cs
│   │   │   │   └── Prompts/
│   │   │   │       ├── EmailReplyPrompt.cs
│   │   │   │       ├── ScheduleExtractionPrompt.cs
│   │   │   │       └── TransactionParsingPrompt.cs
│   │   │   │
│   │   │   ├── BackgroundJobs/
│   │   │   │   ├── EmailCleanupJob.cs
│   │   │   │   ├── AIDraftScanJob.cs
│   │   │   │   ├── ScheduleExtractionJob.cs
│   │   │   │   ├── TransactionScanJob.cs
│   │   │   │   ├── DriveWatchRenewalJob.cs
│   │   │   │   ├── PermissionScanJob.cs
│   │   │   │   ├── BackupJob.cs
│   │   │   │   └── DataRetentionJob.cs
│   │   │   │
│   │   │   ├── Notifications/
│   │   │   │   ├── SignalRNotificationService.cs
│   │   │   │   └── DiscordWebhookService.cs
│   │   │   │
│   │   │   ├── Security/
│   │   │   │   ├── JwtService.cs
│   │   │   │   └── TokenEncryptionService.cs
│   │   │   │
│   │   │   └── DependencyInjection.cs
│   │   │
│   │   ├── GOpsHub.API/                     # 🌐 Presentation Layer
│   │   │   ├── Controllers/
│   │   │   │   ├── AuthController.cs
│   │   │   │   ├── PublicController.cs       # Anonymous access
│   │   │   │   ├── DashboardController.cs
│   │   │   │   ├── EmailOpsController.cs
│   │   │   │   ├── SchedulingController.cs
│   │   │   │   ├── FinanceController.cs
│   │   │   │   ├── DriveGuardController.cs
│   │   │   │   ├── BackupController.cs
│   │   │   │   ├── NotificationsController.cs
│   │   │   │   ├── SettingsController.cs
│   │   │   │   └── WebhookController.cs
│   │   │   │
│   │   │   ├── Hubs/
│   │   │   │   └── NotificationHub.cs
│   │   │   │
│   │   │   ├── Middleware/
│   │   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   │   └── RequestLoggingMiddleware.cs
│   │   │   │
│   │   │   ├── appsettings.json
│   │   │   ├── appsettings.Development.json
│   │   │   └── Program.cs
│   │   │
│   │   └── GOpsHub.Tests/                   # 🧪 Tests
│   │       ├── Unit/
│   │       │   ├── Domain/
│   │       │   └── Application/
│   │       └── Integration/
│   │           └── Api/
│   │
│   └── frontend/                            # 🎨 Vue 3 Frontend
│       ├── package.json
│       ├── vite.config.ts
│       ├── tsconfig.json
│       ├── index.html
│       ├── vercel.json                      # ⚡ Vercel config + API proxy
│       │
│       ├── public/
│       │   └── favicon.ico
│       │
│       └── src/
│           ├── main.ts
│           ├── App.vue
│           │
│           ├── assets/
│           │   └── styles/
│           │       ├── _variables.scss
│           │       ├── _mixins.scss
│           │       └── main.scss
│           │
│           ├── router/
│           │   └── index.ts
│           │
│           ├── stores/
│           │   ├── auth.store.ts
│           │   ├── dashboard.store.ts
│           │   ├── email.store.ts
│           │   ├── calendar.store.ts
│           │   ├── finance.store.ts
│           │   ├── drive.store.ts
│           │   └── notification.store.ts
│           │
│           ├── composables/
│           │   ├── useAuth.ts
│           │   ├── useApi.ts
│           │   ├── useSignalR.ts
│           │   └── useTheme.ts
│           │
│           ├── services/
│           │   ├── api.service.ts
│           │   ├── auth.service.ts
│           │   ├── email.service.ts
│           │   ├── calendar.service.ts
│           │   ├── finance.service.ts
│           │   ├── drive.service.ts
│           │   └── signalr.service.ts
│           │
│           ├── types/
│           │   ├── auth.types.ts
│           │   ├── email.types.ts
│           │   ├── calendar.types.ts
│           │   ├── finance.types.ts
│           │   ├── drive.types.ts
│           │   └── api.types.ts
│           │
│           ├── components/
│           │   ├── common/
│           │   │   ├── AppHeader.vue
│           │   │   ├── AppSidebar.vue
│           │   │   ├── LoadingSpinner.vue
│           │   │   ├── ConfirmDialog.vue
│           │   │   └── StatusBadge.vue
│           │   │
│           │   ├── dashboard/
│           │   │   ├── StatsCards.vue
│           │   │   ├── ActivityFeed.vue
│           │   │   └── QuickActions.vue
│           │   │
│           │   ├── email/
│           │   │   ├── CleanupRuleList.vue
│           │   │   ├── CleanupRuleForm.vue
│           │   │   ├── DraftReviewCard.vue
│           │   │   └── DraftList.vue
│           │   │
│           │   ├── calendar/
│           │   │   ├── ExtractedEventCard.vue
│           │   │   └── ScheduleList.vue
│           │   │
│           │   ├── finance/
│           │   │   ├── TransactionTable.vue
│           │   │   ├── MonthlySummaryChart.vue
│           │   │   └── CategoryPieChart.vue
│           │   │
│           │   └── drive/
│           │       ├── AuditLogTable.vue
│           │       ├── AlertCard.vue
│           │       └── MonitoredFolderList.vue
│           │
│           ├── layouts/
│           │   ├── DefaultLayout.vue        # Admin layout (full sidebar)
│           │   └── PublicLayout.vue          # Anonymous layout (limited)
│           │
│           └── views/
│               ├── LoginView.vue
│               ├── DashboardView.vue
│               ├── EmailOpsView.vue
│               ├── CalendarView.vue
│               ├── FinanceView.vue
│               ├── DriveGuardView.vue
│               ├── SettingsView.vue
│               ├── PublicCalendarView.vue    # Anonymous: busy/free
│               └── NotFoundView.vue
│
├── .github/
│   └── workflows/
│       └── ci.yml
│
├── .env.example
├── .gitignore
├── README.md
└── LICENSE
```

---

## Layer Responsibilities

### Domain Layer (`GOpsHub.Domain`)
- **Zero dependencies** — không phụ thuộc bất kỳ layer nào
- Entities, Value Objects, Enums, Domain Events, Repository Interfaces

### Application Layer (`GOpsHub.Application`)
- Phụ thuộc: Domain only
- **Self-implemented CQRS**: ICommand/IQuery + Handlers + Dispatcher
- Use Case orchestration, DTOs, Validation

### Infrastructure Layer (`GOpsHub.Infrastructure`)
- Phụ thuộc: Domain, Application
- MongoDB persistence, Google API clients, Gemini AI, Hangfire jobs, SignalR

### Presentation Layer (`GOpsHub.API`)
- Phụ thuộc: Application
- Controllers, SignalR Hub, Middleware — thin layer, delegate xuống Application
