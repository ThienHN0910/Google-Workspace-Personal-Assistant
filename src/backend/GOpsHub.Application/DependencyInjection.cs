using System.Reflection;
using FluentValidation;
using GOpsHub.Application.Common.CQRS;
using Microsoft.Extensions.DependencyInjection;

namespace GOpsHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register CQRS Dispatcher
        services.AddScoped<IDispatcher, Dispatcher>();

        // Register all Command and Query Handlers automatically
        var assembly = Assembly.GetExecutingAssembly();

        services.ScanHandlers(assembly, typeof(ICommandHandler<,>));
        services.ScanHandlers(assembly, typeof(ICommandHandler<>));
        services.ScanHandlers(assembly, typeof(IQueryHandler<,>));

        // Register FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    private static void ScanHandlers(this IServiceCollection services, Assembly assembly, Type handlerInterfaceType)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces(), (t, i) => new { Implementation = t, Interface = i })
            .Where(x => x.Interface.IsGenericType && x.Interface.GetGenericTypeDefinition() == handlerInterfaceType);

        foreach (var handler in handlerTypes)
        {
            services.AddScoped(handler.Interface, handler.Implementation);
        }
    }
}
