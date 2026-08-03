using GOpsHub.Application.Common.Interfaces;
using GOpsHub.Domain.Interfaces;
using GOpsHub.Infrastructure.AI;
using GOpsHub.Infrastructure.GoogleApis;
using GOpsHub.Infrastructure.Persistence;
using GOpsHub.Infrastructure.Persistence.Repositories;
using GOpsHub.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GOpsHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // MongoDB Options
        services.Configure<MongoDbSettings>(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? configuration["MongoDB:ConnectionString"] 
                ?? "mongodb://localhost:27017";
            options.DatabaseName = configuration["MongoDB:DatabaseName"] ?? "gopshub";
        });

        // MongoDbContext
        services.AddSingleton<MongoDbContext>();

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

        // Security Services
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ITokenEncryptionService, TokenEncryptionService>();

        // Google & AI Services
        services.AddScoped<IGmailService, GmailApiService>();
        services.AddScoped<IAIService, GeminiAIService>();

        return services;
    }
}
