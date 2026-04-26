using Onboarding.Application.Common;
using Onboarding.Domain.Common;

namespace Onboarding.Application.Admin.Commands;

/// <summary>
/// Command: update company data (ADMIN-03).
/// Full implementation deferred to Phase 38/41.
/// </summary>
public sealed record UpdateCompanyCommand(
    Guid CompanyId,
    string RazaoSocial,
    string Email,
    string Phone);

public sealed class UpdateCompanyCommandHandler : ICommandHandler<UpdateCompanyCommand, Unit>
{
    public Task<Unit> HandleAsync(UpdateCompanyCommand command, CancellationToken ct = default)
        => throw new NotImplementedException("Full implementation in Phase 38/41");
}