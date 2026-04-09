using Onboarding.Application.Common;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command: update user data (ADMIN-03).
/// </summary>
public sealed record UpdateUserCommand(
    Guid UserId,
    string Name,
    string? RazaoSocial,
    string Email,
    string Phone);

public sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, Unit>
{
    public Task<Unit> HandleAsync(UpdateUserCommand command, CancellationToken ct = default)
    {
        throw new NotImplementedException("Handler implementation will be added in Plan 02.");
    }
}
