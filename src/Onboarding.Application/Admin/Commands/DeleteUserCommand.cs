using Onboarding.Application.Common;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command: LGPD-compliant user deletion (ADMIN-05).
/// </summary>
public sealed record DeleteUserCommand(
    Guid UserId,
    string ConfirmEmail);

public sealed class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, Unit>
{
    public Task<Unit> HandleAsync(DeleteUserCommand command, CancellationToken ct = default)
    {
        throw new NotImplementedException("Handler implementation will be added in Plan 02.");
    }
}
