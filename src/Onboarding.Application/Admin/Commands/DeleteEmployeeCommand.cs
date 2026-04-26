using Onboarding.Application.Common;
using Onboarding.Domain.Common;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command: LGPD-compliant employee deletion (ADMIN-05).
/// Full implementation deferred to Phase 38/41.
/// </summary>
public sealed record DeleteEmployeeCommand(
    Guid EmployeeId,
    string ActorSub);

public sealed class DeleteEmployeeCommandHandler : ICommandHandler<DeleteEmployeeCommand, Unit>
{
    public Task<Unit> HandleAsync(DeleteEmployeeCommand command, CancellationToken ct = default)
        => throw new NotImplementedException("Full implementation in Phase 38/41");
}