using Microsoft.Extensions.DependencyInjection;

namespace GOpsHub.Application.Common.CQRS;

/// <summary>
/// Central dispatcher that resolves and invokes the appropriate handler
/// for a given command or query. Replaces MediatR's IMediator.
/// </summary>
public interface IDispatcher
{
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default);
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default);
}

public class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        object? handlerInstance;
        try
        {
            handlerInstance = _serviceProvider.GetRequiredService(handlerType);
        }
        catch (InvalidOperationException ex)
        {
            throw new NotImplementedException($"No command handler registered for '{command.GetType().Name}'. Ensure AddApplicationServices() is configured.", ex);
        }

        dynamic handler = handlerInstance;
        return await handler.HandleAsync((dynamic)command, ct);
    }

    public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        object? handlerInstance;
        try
        {
            handlerInstance = _serviceProvider.GetRequiredService(handlerType);
        }
        catch (InvalidOperationException ex)
        {
            throw new NotImplementedException($"No query handler registered for '{query.GetType().Name}'. Ensure AddApplicationServices() is configured.", ex);
        }

        dynamic handler = handlerInstance;
        return await handler.HandleAsync((dynamic)query, ct);
    }
}
