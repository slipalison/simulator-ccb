using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Infrastructure.Dispatch;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Dispatch;

// --- Test queries and handlers at namespace level so dynamic binder can access them ---

public sealed record QryDispatcherTestQuery(string Filter);
public sealed record QryDispatcherAnotherQuery(Guid Id);

public sealed class QryDispatcherTestHandler : IQueryHandler<QryDispatcherTestQuery, string[]>
{
    public Task<string[]> HandleAsync(QryDispatcherTestQuery query, CancellationToken ct = default)
        => Task.FromResult(new[] { query.Filter });
}

/// <summary>
/// Unit tests for <see cref="QueryDispatcher"/>.
/// Test types are at namespace level so dynamic binder can invoke them.
/// </summary>
public sealed class QueryDispatcherTests
{
    [Fact]
    public async Task Query_RegisteredHandler_InvokesHandlerAndReturnsResult()
    {
        // Arrange
        var sp = BuildProvider(sc =>
            sc.AddScoped<IQueryHandler<QryDispatcherTestQuery, string[]>, QryDispatcherTestHandler>());
        var dispatcher = new QueryDispatcher(sp);

        // Act
        var result = await dispatcher.Query<string[]>(new QryDispatcherTestQuery("abc"));

        // Assert
        result.ShouldBe(new[] { "abc" });
    }

    [Fact]
    public async Task Query_SubstituteHandler_InvokedWithCorrectQuery()
    {
        // Arrange
        var handler = Substitute.For<IQueryHandler<QryDispatcherTestQuery, string[]>>();
        handler.HandleAsync(Arg.Any<QryDispatcherTestQuery>(), Arg.Any<CancellationToken>())
            .Returns(new[] { "sub" });

        var sp = BuildProvider(sc =>
            sc.AddScoped<IQueryHandler<QryDispatcherTestQuery, string[]>>(_ => handler));
        var dispatcher = new QueryDispatcher(sp);

        var qry = new QryDispatcherTestQuery("xyz");

        // Act
        var result = await dispatcher.Query<string[]>(qry);

        // Assert
        result.ShouldBe(new[] { "sub" });
        await handler.Received(1).HandleAsync(
            Arg.Is<QryDispatcherTestQuery>(q => q.Filter == "xyz"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Query_UnregisteredHandler_ThrowsInvalidOperationException()
    {
        // Arrange — no handler registered
        var sp = BuildProvider(_ => { });
        var dispatcher = new QueryDispatcher(sp);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => dispatcher.Query<string>(new QryDispatcherAnotherQuery(Guid.NewGuid())));
    }

    [Fact]
    public async Task Query_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var sp = BuildProvider(_ => { });
        var dispatcher = new QueryDispatcher(sp);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => dispatcher.Query<string>(null!));
    }

    [Fact]
    public async Task Query_CancellationTokenPropagated_HandlerReceivesToken()
    {
        // Arrange
        var handler = Substitute.For<IQueryHandler<QryDispatcherTestQuery, string[]>>();
        handler.HandleAsync(Arg.Any<QryDispatcherTestQuery>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        var sp = BuildProvider(sc =>
            sc.AddScoped<IQueryHandler<QryDispatcherTestQuery, string[]>>(_ => handler));
        var dispatcher = new QueryDispatcher(sp);
        using var cts = new CancellationTokenSource();

        // Act
        await dispatcher.Query<string[]>(new QryDispatcherTestQuery("t"), cts.Token);

        // Assert
        await handler.Received(1).HandleAsync(
            Arg.Any<QryDispatcherTestQuery>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [Fact]
    public async Task Query_TypeCacheWorks_SecondCallDoesNotThrow()
    {
        // Validates ConcurrentDictionary cache path on second call.
        var sp = BuildProvider(sc =>
            sc.AddScoped<IQueryHandler<QryDispatcherTestQuery, string[]>, QryDispatcherTestHandler>());
        var dispatcher = new QueryDispatcher(sp);

        var r1 = await dispatcher.Query<string[]>(new QryDispatcherTestQuery("first"));
        var r2 = await dispatcher.Query<string[]>(new QryDispatcherTestQuery("second"));

        r1.ShouldBe(new[] { "first" });
        r2.ShouldBe(new[] { "second" });
    }

    // --- Helpers ---

    private static IServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var sc = new ServiceCollection();
        configure(sc);
        return sc.BuildServiceProvider();
    }
}
