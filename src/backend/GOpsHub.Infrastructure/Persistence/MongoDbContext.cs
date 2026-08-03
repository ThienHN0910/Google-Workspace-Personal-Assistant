using GOpsHub.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GOpsHub.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string? name = null)
    {
        var collectionName = name ?? GetDefaultCollectionName<T>();
        return _database.GetCollection<T>(collectionName);
    }

    private static string GetDefaultCollectionName<T>()
    {
        var typeName = typeof(T).Name;
        return typeName switch
        {
            nameof(AdminUser) => "admin_user",
            nameof(CleanupRule) => "cleanup_rules",
            nameof(CleanupLog) => "cleanup_logs",
            nameof(AIDraft) => "ai_drafts",
            nameof(ReplyTemplate) => "reply_templates",
            nameof(ExtractedSchedule) => "extracted_schedules",
            nameof(Transaction) => "transactions",
            nameof(MonitoredFolder) => "monitored_folders",
            nameof(DriveAuditLog) => "drive_audit_logs",
            nameof(SecurityAlert) => "security_alerts",
            nameof(Notification) => "notifications",
            nameof(BackupRecord) => "backup_records",
            _ => typeName.ToLowerInvariant() + "s"
        };
    }
}
