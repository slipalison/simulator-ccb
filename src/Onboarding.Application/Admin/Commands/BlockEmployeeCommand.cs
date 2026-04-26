using Onboarding.Application.Common;
using Onboarding.Domain.Common;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command: block employee account (ADMIN-04).
/// Full implementation deferred to Phase 38/41.
/// </summary>
public sealed record BlockEmployeeCommand(
    Guid EmployeeId,
    string ActorSub);

public sealed class BlockEmployeeCommandHandler : ICommandHandler<BlockEmployeeCommand, Unit>
{
    public Task<Unit> HandleAsync(BlockEmployeeCommand command, CancellationToken ct = default)
        => throw new NotImplementedException("Full implementation in Phase 38/41");
}