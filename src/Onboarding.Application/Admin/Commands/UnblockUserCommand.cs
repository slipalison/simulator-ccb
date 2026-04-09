using Onboarding.Application.Common;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command: unblock user account (ADMIN-04).
/// </summary>
public sealed record UnblockUserCommand(Guid UserId);

public sealed class UnblockUserCommandHandler : ICommandHandler<UnblockUserCommand, Unit>
{
    public Task<Unit> HandleAsync(UnblockUserCommand command, CancellationToken ct = default)
    {
        throw new NotImplementedException("Handler implementation will be added in Plan 02.");
    }
}
