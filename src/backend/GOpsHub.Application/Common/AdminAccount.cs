using Microsoft.Extensions.Configuration;

namespace GOpsHub.Application.Common;

public static class AdminAccount
{
    public const string DefaultEmail = "hnt.vn.vn@gmail.com";

    /// <summary>
    /// Reads configuration["ADMIN_EMAIL"] then configuration["Security:AdminEmail"];
    /// falls back to DefaultEmail when blank; returns Trim().ToLowerInvariant().
    /// </summary>
    public static string ResolveEmail(IConfiguration configuration)
    {
        var email = configuration?["ADMIN_EMAIL"];
        if (string.IsNullOrWhiteSpace(email))
        {
            email = configuration?["Security:AdminEmail"];
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            email = DefaultEmail;
        }

        return email.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Case-insensitive comparison between email and the resolved admin email.
    /// </summary>
    public static bool IsAdmin(string? email, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var adminEmail = ResolveEmail(configuration);
        return string.Equals(email.Trim(), adminEmail, StringComparison.OrdinalIgnoreCase);
    }
}
