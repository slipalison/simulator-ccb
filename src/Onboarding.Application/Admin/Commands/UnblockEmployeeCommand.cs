using Onboarding.Application.Common;
using Onboarding.Domain.Common;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command: unblock employee account (ADMIN-04).
/// Full implementation deferred to Phase 38/41.
/// </summary>
public sealed record UnblockEmployeeCommand(
    Guid EmployeeId,
    string ActorSub);

public sealed class UnblockEmployeeCommandHandler : ICommandHandler<UnblockEmployeeCommand, Unit>
{
    public Task<Unit> HandleAsync(UnblockEmployeeCommand command, CancellationToken ct = default)
        => throw new NotImplementedException("Full implementation in Phase 38/41");
}