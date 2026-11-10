using System.Net;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using GOpsHub.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace GOpsHub.Infrastructure.GoogleApis;

public class DriveApiService : IDriveService
{
    private readonly IAdminGoogleCredentialProvider _credentialProvider;
    private readonly ILogger<DriveApiService> _logger;

    public DriveApiService(
        IAdminGoogleCredentialProvider credentialProvider,
        ILogger<DriveApiService> logger)
    {
        _credentialProvider = credentialProvider;
        _logger = logger;
    }

    private async Task<DriveService?> GetDriveClientAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        var credential = await _credentialProvider.GetCredentialAsync("Drive", forceRefresh, ct);
        if (credential == null) return null;

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "G-Ops Hub"
        });
    }

    private Task<DriveService?> GetDriveClientAsync(CancellationToken ct) => GetDriveClientAsync(false, ct);

    internal async Task<T> ExecuteWithRetryAsync<T>(Func<DriveService, Task<T>> action, T defaultValue, CancellationToken ct = default)
    {
        var service = await GetDriveClientAsync(forceRefresh: false, ct);
        if (service == null) return defaultValue;

        try
        {
            return await action(service);
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.Unauthorized || (ex.Error != null && ex.Error.Code == 401))
        {
            _logger.LogWarning("Google Drive API returned 401 Unauthorized. Forcing token refresh and retrying...");
            var freshService = await GetDriveClientAsync(forceRefresh: true, ct);
            if (freshService == null) return defaultValue;

            try
            {
                return await action(freshService);
            }
            catch (Exception retryEx)
            {
                _logger.LogError(retryEx, "Google Drive API retry after token refresh also failed.");
                throw;
            }
        }
    }

    private async Task ExecuteWithRetryAsync(Func<DriveService, Task> action, CancellationToken ct = default)
    {
        var service = await GetDriveClientAsync(forceRefresh: false, ct);
        if (service == null) return;

        try
        {
            await action(service);
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.Unauthorized || (ex.Error != null && ex.Error.Code == 401))
        {
            _logger.LogWarning("Google Drive API returned 401 Unauthorized. Forcing token refresh and retrying...");
            var freshService = await GetDriveClientAsync(forceRefresh: true, ct);
            if (freshService == null) return;

            try
            {
                await action(freshService);
            }
            catch (Exception retryEx)
            {
                _logger.LogError(retryEx, "Google Drive API retry after token refresh also failed.");
                throw;
            }
        }
    }

    public async Task<IReadOnlyList<DriveFileInfo>> ListFilesInFolderAsync(string folderId, CancellationToken ct = default)
    {
        return await ExecuteWithRetryAsync(async service =>
        {
            var request = service.Files.List();
            request.Q = $"'{folderId}' in parents and trashed = false";
            request.Fields = "files(id, name, mimeType, size, modifiedTime, lastModifyingUser(displayName, emailAddress))";

            var result = await request.ExecuteAsync(ct);
            if (result.Files == null) return (IReadOnlyList<DriveFileInfo>)Array.Empty<DriveFileInfo>();

            return (IReadOnlyList<DriveFileInfo>)result.Files.Select(f => new DriveFileInfo
            {
                Id = f.Id,
                Name = f.Name,
                MimeType = f.MimeType,
                Size = f.Size,
                ModifiedTime = f.ModifiedTimeDateTimeOffset?.UtcDateTime,
                LastModifyingUser = f.LastModifyingUser?.DisplayName ?? f.LastModifyingUser?.EmailAddress
            }).ToList();
        }, Array.Empty<DriveFileInfo>(), ct);
    }

    public async Task<DriveFileInfo?> GetFileInfoAsync(string fileId, CancellationToken ct = default)
    {
        return await ExecuteWithRetryAsync(async service =>
        {
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
        }, null, ct);
    }

    public async Task<string?> FindFileByNameAsync(string fileName, string mimeType = "application/vnd.google-apps.spreadsheet", string? folderId = null, CancellationToken ct = default)
    {
        return await ExecuteWithRetryAsync(async service =>
        {
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
        }, null, ct);
    }

    public async Task<string> UploadFileAsync(string folderId, string fileName, Stream content, string mimeType, CancellationToken ct = default)
    {
        return await ExecuteWithRetryAsync(async service =>
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName,
                Parents = new List<string> { folderId }
            };

            var request = service.Files.Create(fileMetadata, content, mimeType);
            await request.UploadAsync(ct);

            return request.ResponseBody?.Id ?? string.Empty;
        }, string.Empty, ct);
    }

    public async Task MoveFileAsync(string fileId, string targetFolderId, CancellationToken ct = default)
    {
        await ExecuteWithRetryAsync(async service =>
        {
            var getReq = service.Files.Get(fileId);
            getReq.Fields = "parents";
            var file = await getReq.ExecuteAsync(ct);

            var previousParents = file.Parents != null ? string.Join(",", file.Parents) : string.Empty;

            var updateReq = service.Files.Update(new Google.Apis.Drive.v3.Data.File(), fileId);
            updateReq.AddParents = targetFolderId;
            if (!string.IsNullOrEmpty(previousParents))
            {
                updateReq.RemoveParents = previousParents;
            }

            await updateReq.ExecuteAsync(ct);
        }, ct);
    }

    public async Task<string> CreateFolderAsync(string parentFolderId, string folderName, CancellationToken ct = default)
    {
        return await ExecuteWithRetryAsync(async service =>
        {
            var folderMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string> { parentFolderId }
            };

            var folder = await service.Files.Create(folderMetadata).ExecuteAsync(ct);
            return folder.Id;
        }, string.Empty, ct);
    }

    public async Task<IReadOnlyList<DrivePermission>> GetFilePermissionsAsync(string fileId, CancellationToken ct = default)
    {
        return await ExecuteWithRetryAsync(async service =>
        {
            var listReq = service.Permissions.List(fileId);
            listReq.Fields = "permissions(id, type, role, emailAddress)";
            var permissions = await listReq.ExecuteAsync(ct);

            if (permissions.Permissions == null) return (IReadOnlyList<DrivePermission>)Array.Empty<DrivePermission>();

            return (IReadOnlyList<DrivePermission>)permissions.Permissions.Select(p => new DrivePermission
            {
                Id = p.Id,
                Type = p.Type,
                Role = p.Role,
                EmailAddress = p.EmailAddress
            }).ToList();
        }, Array.Empty<DrivePermission>(), ct);
    }

    public async Task RevokePermissionAsync(string fileId, string permissionId, CancellationToken ct = default)
    {
        await ExecuteWithRetryAsync(async service =>
        {
            await service.Permissions.Delete(fileId, permissionId).ExecuteAsync(ct);
        }, ct);
    }

    public async Task<string?> SetupWatchAsync(string folderId, string webhookUrl, CancellationToken ct = default)
    {
        return await ExecuteWithRetryAsync(async service =>
        {
            var channel = new Channel
            {
                Id = Guid.NewGuid().ToString(),
                Type = "web_hook",
                Address = webhookUrl
            };

            var watchResp = await service.Files.Watch(channel, folderId).ExecuteAsync(ct);
            return watchResp.Id;
        }, null, ct);
    }

    public async Task<string> EnsureQuarantineFolderAsync(CancellationToken ct = default)
    {
        return await ExecuteWithRetryAsync(async service =>
        {
            var listReq = service.Files.List();
            listReq.Q = "name = 'G-Ops Quarantine' and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
            listReq.Fields = "files(id, name)";
            var listResp = await listReq.ExecuteAsync(ct);

            var existingFolder = listResp.Files?.FirstOrDefault();
            if (existingFolder != null)
            {
                return existingFolder.Id;
            }

            var folderMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = "G-Ops Quarantine",
                MimeType = "application/vnd.google-apps.folder"
            };
            var created = await service.Files.Create(folderMetadata).ExecuteAsync(ct);
            return created.Id;
        }, string.Empty, ct);
    }

    public async Task RestoreFileAsync(string fileId, string originalFolderId, CancellationToken ct = default)
    {
        await MoveFileAsync(fileId, originalFolderId, ct);
    }
}
