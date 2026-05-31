namespace Onboarding.Application.Common;

/// <summary>
/// Resolves the correct <see cref="IQueryHandler{TQuery,TResult}"/> from the DI container
/// by runtime type and invokes <c>HandleAsync</c>.
/// D-60: manual dispatch via IServiceProvider — no MediatR (D-3 OSS-only).
/// </summary>
public interface IQueryDispatcher
{
    /// <summary>
    /// Dispatches <paramref name="query"/> to its registered handler and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The handler result type.</typeparam>
    /// <param name="query">The query instance (runtime type determines the handler).</param>
    /// <param name="ct">Cancellation token propagated to the handler.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <c>IQueryHandler&lt;TQuery, TResult&gt;</c> is registered for the
    /// query's runtime type.
    /// </exception>
    Task<TResult> Query<TResult>(object query, CancellationToken ct = default);
}
