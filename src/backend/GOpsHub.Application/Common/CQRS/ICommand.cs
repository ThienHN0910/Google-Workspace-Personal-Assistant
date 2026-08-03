namespace GOpsHub.Application.Common.CQRS;

/// <summary>
/// Marker interface for a command that returns a result.
/// </summary>
public interface ICommand<TResult> { }

/// <summary>
/// Marker interface for a command that returns nothing.
/// </summary>
public interface ICommand : ICommand<Unit> { }

/// <summary>
/// Represents a void return type for commands.
/// </summary>
public readonly struct Unit
{
    public static readonly Unit Value = new();
}
