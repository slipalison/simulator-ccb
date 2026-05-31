using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Infrastructure.Dispatch;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Dispatch;

/// <summary>
/// Unit tests for <see cref="CommandDispatcher"/>.
/// Uses a real <see cref="ServiceCollection"/> / <see cref="IServiceProvider"/> to verify
/// handler resolution (mock IServiceProvider is not idiomatic for DI containers).
/// </summary>
public sealed class CommandDispatcherTests
{
    // --- Test commands and handlers ---

    private sealed record TestCommand(string Value);
    private sealed record AnotherCommand(int Number);

    private sealed class TestCommandHandler : ICommandHandler<TestCommand, string>
    {
        public Task<string> HandleAsync(TestCommand command, CancellationToken ct = default)
            => Task.FromResult($"handled:{command.Value}");
    }

    // --- Tests ---

    [Fact]
    public async Task Send_RegisteredHandler_InvokesHandlerAndReturnsResult()
    {
        // Arrange
        var sp = BuildProvider(sc =>
            sc.AddScoped<ICommandHandler<TestCommand, string>, TestCommandHandler>());
        var dispatcher = new CommandDispatcher(sp);

        // Act
        var result = await dispatcher.Send<string>(new TestCommand("hello"));

        // Assert
        result.ShouldBe("handled:hello");
    }

    [Fact]
    public async Task Send_SubstituteHandler_InvokedWithCorrectCommand()
    {
        // Arrange
        var handler = Substitute.For<ICommandHandler<TestCommand, string>>();
        handler.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns("substituted");

        var sp = BuildProvider(sc =>
            sc.AddScoped<ICommandHandler<TestCommand, string>>(_ => handler));
        var dispatcher = new CommandDispatcher(sp);

        var cmd = new TestCommand("world");

        // Act
        var result = await dispatcher.Send<string>(cmd);

        // Assert
        result.ShouldBe("substituted");
        await handler.Received(1).HandleAsync(Arg.Is<TestCommand>(c => c.Value == "world"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_UnregisteredHandler_ThrowsInvalidOperationException()
    {
        // Arrange — no handler registered for AnotherCommand
        var sp = BuildProvider(_ => { });
        var dispatcher = new CommandDispatcher(sp);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => dispatcher.Send<int>(new AnotherCommand(42)));
    }

    [Fact]
    public async Task Send_NullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var sp = BuildProvider(_ => { });
        var dispatcher = new CommandDispatcher(sp);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => dispatcher.Send<string>(null!));
    }

    [Fact]
    public async Task Send_CancellationTokenPropagated_HandlerReceivesToken()
    {
        // Arrange
        var handler = Substitute.For<ICommandHandler<TestCommand, string>>();
        handler.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
            .Returns("ok");

        var sp = BuildProvider(sc =>
            sc.AddScoped<ICommandHandler<TestCommand, string>>(_ => handler));
        var dispatcher = new CommandDispatcher(sp);
        using var cts = new CancellationTokenSource();

        // Act
        await dispatcher.Send<string>(new TestCommand("x"), cts.Token);

        // Assert
        await handler.Received(1).HandleAsync(
            Arg.Any<TestCommand>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [Fact]
    public async Task Send_TypeCacheWorks_SecondCallDoesNotThrow()
    {
        // Validates the ConcurrentDictionary type-cache path is hit on the second call.
        var sp = BuildProvider(sc =>
            sc.AddScoped<ICommandHandler<TestCommand, string>, TestCommandHandler>());
        var dispatcher = new CommandDispatcher(sp);

        // Act — two calls, second goes through cache
        var r1 = await dispatcher.Send<string>(new TestCommand("first"));
        var r2 = await dispatcher.Send<string>(new TestCommand("second"));

        // Assert
        r1.ShouldBe("handled:first");
        r2.ShouldBe("handled:second");
    }

    // --- Helpers ---

    private static IServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var sc = new ServiceCollection();
        configure(sc);
        return sc.BuildServiceProvider();
    }
}
