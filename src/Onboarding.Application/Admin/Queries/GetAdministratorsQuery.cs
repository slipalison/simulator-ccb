using Onboarding.Application.Common;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: retorna todos os administradores via Keycloak.
/// Sem paginacao — lista de admins e pequena.
/// </summary>
public sealed record GetAdministratorsQuery();

public sealed class GetAdministratorsQueryHandler
    : IQueryHandler<GetAdministratorsQuery, IReadOnlyList<AdminUserDto>>
{
    private readonly IKeycloakUserService _keycloakUserService;

    public GetAdministratorsQueryHandler(IKeycloakUserService keycloakUserService)
        => _keycloakUserService = keycloakUserService;

    public async Task<IReadOnlyList<AdminUserDto>> HandleAsync(
        GetAdministratorsQuery query, CancellationToken ct = default)
        => await _keycloakUserService.GetUsersByRoleAsync("backoffice", "admin", ct);
}
