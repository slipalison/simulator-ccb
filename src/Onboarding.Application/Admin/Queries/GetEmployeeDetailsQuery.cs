using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Common;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: detailed employee data (ADMIN-02).
/// Full implementation deferred to Phase 38/41.
/// </summary>
public sealed record GetEmployeeDetailsQuery(Guid EmployeeId)
    : IQuery<EmployeeSummaryDto>;

public sealed class GetEmployeeDetailsHandler
    : IQueryHandler<GetEmployeeDetailsQuery, EmployeeSummaryDto>
{
    public Task<EmployeeSummaryDto> HandleAsync(
        GetEmployeeDetailsQuery query, CancellationToken ct = default)
        => throw new NotImplementedException("Full implementation in Phase 38/41");
}