using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Application.Common;

namespace Onboarding.Infrastructure.Dispatch;

/// <summary>
/// Resolves <c>IQueryHandler&lt;TQuery, TResult&gt;</c> from <see cref="IServiceProvider"/>
/// by the runtime type of the query and invokes <c>HandleAsync</c>.
///
/// Type resolution is cached in a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// (queryType, resultType) so the reflection cost is paid only once per type pair.
///
/// D-60: manual dispatch — no MediatR (D-3 OSS-only). KISS — no source generator.
/// </summary>
internal sealed class QueryDispatcher(IServiceProvider sp) : IQueryDispatcher
{
    // Cache: (queryType, resultType) -> closed IQueryHandler<TQuery, TResult> type.
    private static readonly ConcurrentDictionary<(Type, Type), Type> HandlerTypeCache = new();

    public Task<TResult> Query<TResult>(object query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var queryType = query.GetType();
        var handlerType = HandlerTypeCache.GetOrAdd(
            (queryType, typeof(TResult)),
            static key => typeof(IQueryHandler<,>).MakeGenericType(key.Item1, key.Item2));

        var handler = sp.GetService(handlerType)
            ?? throw new InvalidOperationException(
                $"No handler registered for IQueryHandler<{queryType.Name}, {typeof(TResult).Name}>. " +
                "Ensure it is registered in the DI container.");

        dynamic h = handler;
        return h.HandleAsync((dynamic)query, ct);
    }
}
