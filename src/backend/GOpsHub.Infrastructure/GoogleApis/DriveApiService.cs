using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Entities;
using GOpsHub.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Infrastructure.GoogleApis;

public class DriveApiService : IDriveService
{
    private readonly IRepository<AdminUser> _userRepo;
    private readonly ITokenEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DriveApiService> _logger;
    private readonly string _adminEmail;

    public DriveApiService(
        IRepository<AdminUser> userRepo,
        ITokenEncryptionService encryptionService,
        IConfiguration configuration,
        ILogger<DriveApiService> logger)
    {
        _userRepo = userRepo;
        _encryptionService = encryptionService;
        _configuration = configuration;
        _logger = logger;
        _adminEmail = _configuration["ADMIN_EMAIL"] ?? "hnt.vn.vn@gmail.com";
    }

    private async Task<DriveService?> GetDriveClientAsync(CancellationToken ct = default)
    {
        var user = await _userRepo.FindOneAsync(u => u.Email == _adminEmail, ct);
        if (user == null || string.IsNullOrEmpty(user.GoogleAccessToken))
        {
            _logger.LogWarning("Admin user token not found for Drive API calls.");
            return null;
        }

        var accessToken = _encryptionService.Decrypt(user.GoogleAccessToken);
        var credential = GoogleCredential.FromAccessToken(accessToken);

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "G-Ops Hub"
        });
    }

    public async Task<IReadOnlyList<DriveFileInfo>> ListFilesInFolderAsync(string folderId, CancellationToken ct = default)
    {
        var service = await GetDriveClientAsync(ct);
        if (service == null) return Array.Empty<DriveFileInfo>();

        var request = service.Files.List();
        request.Q = $"'{folderId}' in parents and trashed = false";
        request.Fields = "files(id, name, mimeType, size, modifiedTime, lastModifyingUser(displayName, emailAddress))";

        var result = await request.ExecuteAsync(ct);
        if (result.Files == null) return Array.Empty<DriveFileInfo>();

        return result.Files.Select(f => new DriveFileInfo
        {
            Id = f.Id,
            Name = f.Name,
            MimeType = f.MimeType,
            Size = f.Size,
            ModifiedTime = f.ModifiedTimeDateTimeOffset?.UtcDateTime,
            LastModifyingUser = f.LastModifyingUser?.DisplayName ?? f.LastModifyingUser?.EmailAddress
        }).ToList();
    }

    public async Task<DriveFileInfo?> GetFileInfoAsync(string fileId, CancellationToken ct = default)
    {
        var service = await GetDriveClientAsync(ct);
        if (service == null) return null;

        var f = await service.Files.Get(fileId).ExecuteAsync(ct);
        if (f == null) return null;

        return new DriveFileInfo
        {
            Id = f.Id,
            Name = f.Name,
            MimeType = f.MimeType,
            Size = f.Size,
            ModifiedTime = f.ModifiedTimeDateTimeOffset?.UtcDateTime
        };
    }

    public async Task<string?> FindFileByNameAsync(string fileName, string mimeType = "application/vnd.google-apps.spreadsheet", string? folderId = null, CancellationToken ct = default)
    {
        var service = await GetDriveClientAsync(ct);
        if (service == null) return null;

        var request = service.Files.List();
        var query = $"name = '{fileName}' and mimeType = '{mimeType}' and trashed = false";
        if (!string.IsNullOrEmpty(folderId))
        {
            query += $" and '{folderId}' in parents";
        }
        request.Q = query;
        request.Fields = "files(id, name)";

        var result = await request.ExecuteAsync(ct);
        return result.Files?.FirstOrDefault()?.Id;
    }

    public async Task<string> UploadFileAsync(string folderId, string fileName, Stream content, string mimeType, CancellationToken ct = default)
    {
        var service = await GetDriveClientAsync(ct);
        if (service == null) return string.Empty;

        var fileMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = fileName,
            Parents = new List<string> { folderId }
        };

        var request = service.Files.Create(fileMetadata, content, mimeType);
        await request.UploadAsync(ct);

        return request.ResponseBody?.Id ?? string.Empty;
    }

    public async Task MoveFileAsync(string fileId, string targetFolderId, CancellationToken ct = default)
    {
        var service = await GetDriveClientAsync(ct);
        if (service == null) return;

        var getReq = service.Files.Get(fileId);
        getReq.Fields = "parents";
        var file = await getReq.ExecuteAsync(ct);

        var previousParents = string.Join(",", file.Parents);

        var updateReq = service.Files.Update(new Google.Apis.Drive.v3.Data.File(), fileId);
        updateReq.AddParents = targetFolderId;
        updateReq.RemoveParents = previousParents;

        await updateReq.ExecuteAsync(ct);
    }

    public async Task<string> CreateFolderAsync(string parentFolderId, string folderName, CancellationToken ct = default)
    {
        var service = await GetDriveClientAsync(ct);
        if (service == null) return string.Empty;

        var folderMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = folderName,
            MimeType = "application/vnd.google-apps.folder",
            Parents = new List<string> { parentFolderId }
        };

        var folder = await service.Files.Create(folderMetadata).ExecuteAsync(ct);
        return folder.Id;
    }

    public async Task<IReadOnlyList<DrivePermission>> GetFilePermissionsAsync(string fileId, CancellationToken ct = default)
    {
        var service = await GetDriveClientAsync(ct);
        if (service == null) return Array.Empty<DrivePermission>();

        var listReq = service.Permissions.List(fileId);
        listReq.Fields = "permissions(id, type, role, emailAddress)";
        var permissions = await listReq.ExecuteAsync(ct);

        if (permissions.Permissions == null) return Array.Empty<DrivePermission>();

        return permissions.Permissions.Select(p => new DrivePermission
        {
            Id = p.Id,
            Type = p.Type,
            Role = p.Role,
            EmailAddress = p.EmailAddress
        }).ToList();
    }

    public async Task RevokePermissionAsync(string fileId, string permissionId, CancellationToken ct = default)
    {
        var service = await GetDriveClientAsync(ct);
        if (service == null) return;

        await service.Permissions.Delete(fileId, permissionId).ExecuteAsync(ct);
    }

    public async Task<string?> SetupWatchAsync(string folderId, string webhookUrl, CancellationToken ct = default)
    {
        var service = await GetDriveClientAsync(ct);
        if (service == null) return null;

        var channel = new Channel
        {
            Id = Guid.NewGuid().ToString(),
            Type = "web_hook",
            Address = webhookUrl
        };

        var watchResp = await service.Files.Watch(channel, folderId).ExecuteAsync(ct);
        return watchResp.Id;
    }
}
