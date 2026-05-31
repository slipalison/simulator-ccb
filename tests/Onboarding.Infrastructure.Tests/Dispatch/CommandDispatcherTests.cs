using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Infrastructure.Dispatch;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Dispatch;

// --- Test commands and handlers at namespace level so dynamic binder can access them ---

public sealed record CmdDispatcherTestCommand(string Value);
public sealed record CmdDispatcherAnotherCommand(int Number);

public sealed class CmdDispatcherTestHandler : ICommandHandler<CmdDispatcherTestCommand, string>
{
    public Task<string> HandleAsync(CmdDispatcherTestCommand command, CancellationToken ct = default)
        => Task.FromResult($"handled:{command.Value}");
}

/// <summary>
/// Unit tests for <see cref="CommandDispatcher"/>.
/// Uses a real <see cref="ServiceCollection"/> / <see cref="IServiceProvider"/> to verify
/// handler resolution. Test types are at namespace level so dynamic binder can invoke them.
/// </summary>
public sealed class CommandDispatcherTests
{
    [Fact]
    public async Task Send_RegisteredHandler_InvokesHandlerAndReturnsResult()
    {
        // Arrange
        var sp = BuildProvider(sc =>
            sc.AddScoped<ICommandHandler<CmdDispatcherTestCommand, string>, CmdDispatcherTestHandler>());
        var dispatcher = new CommandDispatcher(sp);

        // Act
        var result = await dispatcher.Send<string>(new CmdDispatcherTestCommand("hello"));

        // Assert
        result.ShouldBe("handled:hello");
    }

    [Fact]
    public async Task Send_SubstituteHandler_InvokedWithCorrectCommand()
    {
        // Arrange
        var handler = Substitute.For<ICommandHandler<CmdDispatcherTestCommand, string>>();
        handler.HandleAsync(Arg.Any<CmdDispatcherTestCommand>(), Arg.Any<CancellationToken>())
            .Returns("substituted");

        var sp = BuildProvider(sc =>
            sc.AddScoped<ICommandHandler<CmdDispatcherTestCommand, string>>(_ => handler));
        var dispatcher = new CommandDispatcher(sp);

        var cmd = new CmdDispatcherTestCommand("world");

        // Act
        var result = await dispatcher.Send<string>(cmd);

        // Assert
        result.ShouldBe("substituted");
        await handler.Received(1).HandleAsync(Arg.Is<CmdDispatcherTestCommand>(c => c.Value == "world"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_UnregisteredHandler_ThrowsInvalidOperationException()
    {
        // Arrange — no handler registered for AnotherCommand
        var sp = BuildProvider(_ => { });
        var dispatcher = new CommandDispatcher(sp);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => dispatcher.Send<int>(new CmdDispatcherAnotherCommand(42)));
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
        var handler = Substitute.For<ICommandHandler<CmdDispatcherTestCommand, string>>();
        handler.HandleAsync(Arg.Any<CmdDispatcherTestCommand>(), Arg.Any<CancellationToken>())
            .Returns("ok");

        var sp = BuildProvider(sc =>
            sc.AddScoped<ICommandHandler<CmdDispatcherTestCommand, string>>(_ => handler));
        var dispatcher = new CommandDispatcher(sp);
        using var cts = new CancellationTokenSource();

        // Act
        await dispatcher.Send<string>(new CmdDispatcherTestCommand("x"), cts.Token);

        // Assert
        await handler.Received(1).HandleAsync(
            Arg.Any<CmdDispatcherTestCommand>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    [Fact]
    public async Task Send_TypeCacheWorks_SecondCallDoesNotThrow()
    {
        // Validates the ConcurrentDictionary type-cache path is hit on the second call.
        var sp = BuildProvider(sc =>
            sc.AddScoped<ICommandHandler<CmdDispatcherTestCommand, string>, CmdDispatcherTestHandler>());
        var dispatcher = new CommandDispatcher(sp);

        // Act — two calls, second goes through cache
        var r1 = await dispatcher.Send<string>(new CmdDispatcherTestCommand("first"));
        var r2 = await dispatcher.Send<string>(new CmdDispatcherTestCommand("second"));

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
