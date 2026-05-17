using Onboarding.Application.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Fundos.Queries.GetFundoAllowedTransitions;

/// <summary>
/// Returns the allowed next statuses for a Fundo in the current tenant (D-25).
/// Returns null when the Fundo does not exist or belongs to another tenant (multi-tenant guard D-5).
/// </summary>
public sealed class GetFundoAllowedTransitionsQueryHandler
    : IQueryHandler<GetFundoAllowedTransitionsQuery, IReadOnlyList<string>?>
{
    private readonly IFundoRepository _fundoRepository;
    private readonly ICurrentCompanyService _currentCompanyService;

    public GetFundoAllowedTransitionsQueryHandler(
        IFundoRepository fundoRepository,
        ICurrentCompanyService currentCompanyService)
    {
        _fundoRepository = fundoRepository;
        _currentCompanyService = currentCompanyService;
    }

    public async Task<IReadOnlyList<string>?> HandleAsync(
        GetFundoAllowedTransitionsQuery query, CancellationToken ct = default)
    {
        var fundo = await _fundoRepository.GetByIdAsync(query.FundoId, ct).ConfigureAwait(false);

        // Security: cross-tenant guard — return null (controller maps to 404)
        if (fundo is null || fundo.ClienteId != _currentCompanyService.CompanyId)
            return null;

        return fundo.GetAllowedNextStates();
    }
}
