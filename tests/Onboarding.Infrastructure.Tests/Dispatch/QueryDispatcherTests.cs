using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Infrastructure.Dispatch;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Dispatch;

/// <summary>
/// Unit tests for <see cref="QueryDispatcher"/>.
/// </summary>
public sealed class QueryDispatcherTests
{
    // --- Test queries and handlers ---

    private sealed record TestQuery(string Filter);
    private sealed record AnotherQuery(Guid Id);

    private sealed class TestQueryHandler : IQueryHandler<TestQuery, string[]>
    {
        public Task<string[]> HandleAsync(TestQuery query, CancellationToken ct = default)
            => Task.FromResult(new[] { query.Filter });
    }

    // --- Tests ---

    [Fact]
    public async Task Query_RegisteredHandler_InvokesHandlerAndReturnsResult()
    {
        // Arrange
        var sp = BuildProvider(sc =>
            sc.AddScoped<IQueryHandler<TestQuery, string[]>, TestQueryHandler>());
        var dispatcher = new QueryDispatcher(sp);

        // Act
        var result = await dispatcher.Query<string[]>(new TestQuery("abc"));

        // Assert
        result.ShouldBe(new[] { "abc" });
    }

    [Fact]
    public async Task Query_SubstituteHandler_InvokedWithCorrectQuery()
    {
        // Arrange
        var handler = Substitute.For<IQueryHandler<TestQuery, string[]>>();
        handler.HandleAsync(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>())
            .Returns(new[] { "sub" });

        var sp = BuildProvider(sc =>
            sc.AddScoped<IQueryHandler<TestQuery, string[]>>(_ => handler));
        var dispatcher = new QueryDispatcher(sp);

        var qry = new TestQuery("xyz");

        // Act
        var result = await dispatcher.Query<string[]>(qry);

        // Assert
        result.ShouldBe(new[] { "sub" });
        await handler.Received(1).HandleAsync(
            Arg.Is<TestQuery>(q => q.Filter == "xyz"),
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
            () => dispatcher.Query<string>(new AnotherQuery(Guid.NewGuid())));
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
        var handler = Substitute.For<IQueryHandler<TestQuery, string[]>>();
        handler.HandleAsync(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        var sp = BuildProvider(sc =>
            sc.AddScoped<IQueryHandler<TestQuery, string[]>>(_ => handler));
        var dispatcher = new QueryDispatcher(sp);
        using var cts = new CancellationTokenSource();

        // Act
        await dispatcher.Query<string[]>(new TestQuery("t"), cts.Token);

        // Assert
        await handler.Received(1).HandleAsync(
            Arg.Any<TestQuery>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [Fact]
    public async Task Query_TypeCacheWorks_SecondCallDoesNotThrow()
    {
        // Validates ConcurrentDictionary cache path on second call.
        var sp = BuildProvider(sc =>
            sc.AddScoped<IQueryHandler<TestQuery, string[]>, TestQueryHandler>());
        var dispatcher = new QueryDispatcher(sp);

        var r1 = await dispatcher.Query<string[]>(new TestQuery("first"));
        var r2 = await dispatcher.Query<string[]>(new TestQuery("second"));

        r1.ShouldBe(new[] { "first" });
        r2.ShouldBe(new[] { "second" });
    }
}
