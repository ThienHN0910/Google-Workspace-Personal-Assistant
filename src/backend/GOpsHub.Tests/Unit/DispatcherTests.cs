using FluentAssertions;
using GOpsHub.Application.Common.CQRS;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GOpsHub.Tests.Unit;

public record TestCommand(string Value) : ICommand<string>;
public class TestCommandHandler : ICommandHandler<TestCommand, string>
{
    public Task<string> HandleAsync(TestCommand command, CancellationToken ct = default)
    {
        return Task.FromResult($"Processed: {command.Value}");
    }
}

public record UnregisteredCommand : ICommand<bool>;
public record UnregisteredQuery : IQuery<int>;

public class DispatcherTests
{
    [Fact]
    public async Task SendAsync_WhenHandlerRegistered_ResolvesAndExecutes()
    {
        var services = new ServiceCollection();
        services.AddTransient<ICommandHandler<TestCommand, string>, TestCommandHandler>();
        var provider = services.BuildServiceProvider();

        var dispatcher = new Dispatcher(provider);
        var result = await dispatcher.SendAsync(new TestCommand("hello"));

        result.Should().Be("Processed: hello");
    }

    [Fact]
    public async Task SendAsync_WhenHandlerNotRegistered_ThrowsDescriptiveNotImplementedException()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var dispatcher = new Dispatcher(provider);
        var act = () => dispatcher.SendAsync(new UnregisteredCommand());

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*No command handler registered for 'UnregisteredCommand'*");
    }

    [Fact]
    public async Task QueryAsync_WhenHandlerNotRegistered_ThrowsDescriptiveNotImplementedException()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var dispatcher = new Dispatcher(provider);
        var act = () => dispatcher.QueryAsync(new UnregisteredQuery());

        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*No query handler registered for 'UnregisteredQuery'*");
    }
}
