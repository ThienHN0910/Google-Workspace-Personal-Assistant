using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Interfaces;
using GOpsHub.Infrastructure.AI;
using GOpsHub.Infrastructure.Alerting;
using GOpsHub.Infrastructure.GoogleApis;
using GOpsHub.Infrastructure.Persistence;
using GOpsHub.Infrastructure.Persistence.Repositories;
using GOpsHub.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;

namespace GOpsHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // MongoDB Options
        services.Configure<MongoDbSettings>(options =>
        {
            options.ConnectionString = configuration["MONGODB_CONNECTION_STRING"]
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? configuration["MongoDB:ConnectionString"];

            options.DatabaseName = configuration["MONGODB_DATABASE_NAME"]
                ?? configuration["MongoDB:DatabaseName"];
        });

        // MongoDbContext
        services.AddSingleton<MongoDbContext>();

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

        // Security Services
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ITokenEncryptionService, TokenEncryptionService>();
        services.AddScoped<IGoogleTokenService, GoogleTokenService>();

        // Google, AI & Alerting Services
        services.AddScoped<IGmailService, GmailApiService>();
        services.AddScoped<ICalendarService, CalendarApiService>();
        services.AddScoped<ISheetsService, SheetsApiService>();
        services.AddScoped<IDriveService, DriveApiService>();
        services.AddScoped<ITasksService, TasksApiService>();
        services.AddScoped<IAIService, GeminiAIService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<GOpsHub.Application.Features.DriveGuard.DriveGuardBackgroundJob>();

        // Hangfire Setup
        var mongoConnString = configuration["MONGODB_CONNECTION_STRING"]
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? configuration["MongoDB:ConnectionString"];

        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_18);
            config.UseSimpleAssemblyNameTypeSerializer();
            config.UseRecommendedSerializerSettings();
            
            var migrationOptions = new MongoMigrationOptions
            {
                MigrationStrategy = new MigrateMongoMigrationStrategy(),
                BackupStrategy = new CollectionMongoBackupStrategy()
            };

            config.UseMongoStorage(mongoConnString, "gopshub_hangfire", new MongoStorageOptions
            {
                MigrationOptions = migrationOptions,
                Prefix = "hangfire",
                CheckConnection = false
            });
        });
        
        services.AddHangfireServer();

        return services;
    }
}
