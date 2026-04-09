using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Common;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: detailed user data including Keycloak status (ADMIN-02).
/// </summary>
public sealed record GetUserDetailsQuery(Guid UserId)
    : IQuery<UserDetailDto>;

public sealed class GetUserDetailsHandler
    : IQueryHandler<GetUserDetailsQuery, UserDetailDto>
{
    public Task<UserDetailDto> HandleAsync(
        GetUserDetailsQuery query, CancellationToken ct = default)
    {
        throw new NotImplementedException("Handler implementation will be added in Plan 02.");
    }
}
