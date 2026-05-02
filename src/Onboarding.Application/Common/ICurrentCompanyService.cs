namespace Onboarding.Application.Common;

/// <summary>
/// Provides the current company ID from JWT claims for EF Core HasQueryFilter injection (D-17).
/// Set per-request from JWT sub claim. Prevents cross-company data access at ORM level.
/// </summary>
public interface ICurrentCompanyService
{
    Guid CompanyId { get; }
}