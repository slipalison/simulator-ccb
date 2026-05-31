using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Application.Common;

namespace Onboarding.Infrastructure.Dispatch;

/// <summary>
/// Resolves <c>ICommandHandler&lt;TCommand, TResult&gt;</c> from <see cref="IServiceProvider"/>
/// by the runtime type of the command and invokes <c>HandleAsync</c>.
///
/// Type resolution is cached in a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// (commandType, resultType) so the reflection cost is paid only once per type pair.
///
/// D-60: manual dispatch — no MediatR (D-3 OSS-only). KISS — no source generator.
/// </summary>
internal sealed class CommandDispatcher(IServiceProvider sp) : ICommandDispatcher
{
    // Cache: (commandType, resultType) -> closed ICommandHandler<TCommand, TResult> type.
    private static readonly ConcurrentDictionary<(Type, Type), Type> HandlerTypeCache = new();

    public Task<TResult> Send<TResult>(object command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandType = command.GetType();
        var handlerType = HandlerTypeCache.GetOrAdd(
            (commandType, typeof(TResult)),
            static key => typeof(ICommandHandler<,>).MakeGenericType(key.Item1, key.Item2));

        var handler = sp.GetService(handlerType)
            ?? throw new InvalidOperationException(
                $"No handler registered for ICommandHandler<{commandType.Name}, {typeof(TResult).Name}>. " +
                "Ensure it is registered in the DI container.");

        dynamic h = handler;
        return h.HandleAsync((dynamic)command, ct);
    }
}
