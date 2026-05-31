using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Application.Common;

namespace Onboarding.Infrastructure.Dispatch;

/// <summary>
/// Resolves <c>IQueryHandler&lt;TQuery, TResult&gt;</c> from <see cref="IServiceProvider"/>
/// by the runtime type of the query and invokes <c>HandleAsync</c> via cached reflection.
///
/// Type resolution and MethodInfo are cached in <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// keyed by (queryType, resultType) so reflection cost is paid only once per type pair.
///
/// D-60: manual dispatch — no MediatR (D-3 OSS-only). KISS — no source generator.
/// Reflection instead of dynamic to ensure internal test types work correctly.
/// </summary>
internal sealed class QueryDispatcher(IServiceProvider sp) : IQueryDispatcher
{
    // Cache: (queryType, resultType) -> (closed handler type, HandleAsync MethodInfo)
    private static readonly ConcurrentDictionary<(Type, Type), (Type HandlerType, MethodInfo Method)> Cache = new();

    public Task<TResult> Query<TResult>(object query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var queryType = query.GetType();
        var (handlerType, method) = Cache.GetOrAdd(
            (queryType, typeof(TResult)),
            static key =>
            {
                var ht = typeof(IQueryHandler<,>).MakeGenericType(key.Item1, key.Item2);
                var mi = ht.GetMethod("HandleAsync")
                    ?? throw new InvalidOperationException($"HandleAsync not found on {ht.Name}.");
                return (ht, mi);
            });

        var handler = sp.GetService(handlerType)
            ?? throw new InvalidOperationException(
                $"No handler registered for IQueryHandler<{queryType.Name}, {typeof(TResult).Name}>. " +
                "Ensure it is registered in the DI container.");

        // Invoke via reflection — avoids dynamic binder limitations with internal types in tests
        return (Task<TResult>)method.Invoke(handler, [query, ct])!;
    }
}
