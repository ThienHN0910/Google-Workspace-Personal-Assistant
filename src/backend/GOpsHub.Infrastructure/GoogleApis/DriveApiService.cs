using System.Net;
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
    private readonly IGoogleTokenService _googleTokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DriveApiService> _logger;
    private readonly string _adminEmail;

    public DriveApiService(
        IRepository<AdminUser> userRepo,
        ITokenEncryptionService encryptionService,
        IGoogleTokenService googleTokenService,
        IConfiguration configuration,
        ILogger<DriveApiService> logger)
    {
        _userRepo = userRepo;
        _encryptionService = encryptionService;
        _googleTokenService = googleTokenService;
        _configuration = configuration;
        _logger = logger;
        _adminEmail = _configuration["ADMIN_EMAIL"] ?? "hnt.vn.vn@gmail.com";
    }

    internal async Task<string?> EnsureFreshTokenAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        var user = await _userRepo.FindOneAsync(u => u.Email == _adminEmail, ct);
        if (user == null || string.IsNullOrEmpty(user.GoogleAccessToken))
        {
            _logger.LogWarning("Admin user token not found for Drive API calls.");
            return null;
        }

        var accessToken = _encryptionService.Decrypt(user.GoogleAccessToken);

        // Auto-refresh token if expired, expiring within 5 minutes, or forced
        var isExpiringSoon = !user.GoogleTokenExpiresAt.HasValue || user.GoogleTokenExpiresAt.Value <= DateTime.UtcNow.AddMinutes(5);
        if (forceRefresh || isExpiringSoon)
        {
            if (!string.IsNullOrEmpty(user.GoogleRefreshToken))
            {
                try
                {
                    var refreshToken = _encryptionService.Decrypt(user.GoogleRefreshToken);
                    var newTokens = await _googleTokenService.RefreshAccessTokenAsync(refreshToken, ct);

                    accessToken = newTokens.AccessToken;
                    user.GoogleAccessToken = _encryptionService.Encrypt(newTokens.AccessToken);
                    user.GoogleTokenExpiresAt = DateTime.UtcNow.AddSeconds(newTokens.ExpiresInSeconds);
                    await _userRepo.UpdateAsync(user, ct);

                    _logger.LogInformation("Successfully refreshed Google access token for Drive API.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh Google access token for Drive. User may need to re-authenticate.");
                    if (forceRefresh) return null;
                }
            }
            else
            {
                _logger.LogWarning("Google access token expired and no refresh token available for Drive.");
                if (isExpiringSoon && user.GoogleTokenExpiresAt.HasValue && user.GoogleTokenExpiresAt.Value <= DateTime.UtcNow)
                {
                    return null;
                }
            }
        }

        return accessToken;
    }

    private async Task<DriveService?> GetDriveClientAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        var accessToken = await EnsureFreshTokenAsync(forceRefresh, ct);
        if (string.IsNullOrEmpty(accessToken)) return null;

        var credential = GoogleCredential.FromAccessToken(accessToken);

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "G-Ops Hub"
        });
    }

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
