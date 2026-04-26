using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Common;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: detailed company data (ADMIN-02).
/// Full implementation deferred to Phase 38/41.
/// </summary>
public sealed record GetCompanyDetailsQuery(Guid CompanyId)
    : IQuery<CompanySummaryDto>;

public sealed class GetCompanyDetailsHandler
    : IQueryHandler<GetCompanyDetailsQuery, CompanySummaryDto>
{
    public Task<CompanySummaryDto> HandleAsync(
        GetCompanyDetailsQuery query, CancellationToken ct = default)
        => throw new NotImplementedException("Full implementation in Phase 38/41");
}