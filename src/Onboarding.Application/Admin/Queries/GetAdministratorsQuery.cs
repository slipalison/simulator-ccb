using Onboarding.Application.Common;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: retorna todos os administradores via Keycloak (sem paginação).
/// Usado pelo endpoint legado GET /api/admin/administrators.
/// Para lista paginada+filtrada use GetPaginatedAdministratorsQuery (Phase 35).
/// </summary>
public sealed record GetAdministratorsQuery;

public sealed class GetAdministratorsQueryHandler
    : IQueryHandler<GetAdministratorsQuery, IReadOnlyList<AdminUserDto>>
{
    private readonly IKeycloakUserService _keycloakUserService;

    public GetAdministratorsQueryHandler(IKeycloakUserService keycloakUserService)
        => _keycloakUserService = keycloakUserService;

    public async Task<IReadOnlyList<AdminUserDto>> HandleAsync(
        GetAdministratorsQuery query, CancellationToken ct = default)
    {
        return await _keycloakUserService.GetUsersByRoleAsync("backoffice", "admin", ct);
    }
}
