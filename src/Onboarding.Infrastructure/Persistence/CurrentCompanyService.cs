using Onboarding.Application.Common;

namespace Onboarding.Infrastructure.Persistence;

/// <summary>
/// Scoped service that holds the current company ID for HasQueryFilter injection (D-17).
/// CompanyId is set per-request from JWT claims in Phase 39.
/// Defaults to Guid.Empty when not set — HasQueryFilter will return no data for unauthenticated requests.
/// </summary>
public sealed class CurrentCompanyService : ICurrentCompanyService
{
    public Guid CompanyId { get; set; }
    Guid ICurrentCompanyService.CompanyId => CompanyId;
}