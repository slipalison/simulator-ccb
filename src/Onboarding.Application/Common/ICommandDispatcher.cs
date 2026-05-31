namespace Onboarding.Application.Common;

/// <summary>
/// Resolves the correct <see cref="ICommandHandler{TCommand,TResult}"/> from the DI container
/// by runtime type and invokes <c>HandleAsync</c>.
/// D-60: manual dispatch via IServiceProvider — no MediatR (D-3 OSS-only).
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Dispatches <paramref name="command"/> to its registered handler and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The handler result type.</typeparam>
    /// <param name="command">The command instance (runtime type determines the handler).</param>
    /// <param name="ct">Cancellation token propagated to the handler.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <c>ICommandHandler&lt;TCommand, TResult&gt;</c> is registered for the
    /// command's runtime type.
    /// </exception>
    Task<TResult> Send<TResult>(object command, CancellationToken ct = default);
}
