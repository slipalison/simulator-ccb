using Onboarding.Application.Common;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command: block user account (ADMIN-04).
/// </summary>
public sealed record BlockUserCommand(Guid UserId);

public sealed class BlockUserCommandHandler : ICommandHandler<BlockUserCommand, Unit>
{
    public Task<Unit> HandleAsync(BlockUserCommand command, CancellationToken ct = default)
    {
        throw new NotImplementedException("Handler implementation will be added in Plan 02.");
    }
}
