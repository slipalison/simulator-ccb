using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: detailed company data including Keycloak status (ADMIN-02).
/// </summary>
public sealed record GetUserDetailsQuery(Guid UserId)
    : IQuery<UserDetailDto>;

public sealed class GetUserDetailsHandler
    : IQueryHandler<GetUserDetailsQuery, UserDetailDto>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IKeycloakUserService _keycloakUserService;

    public GetUserDetailsHandler(
        IAdminRepository adminRepository,
        IKeycloakUserService keycloakUserService)
    {
        _adminRepository = adminRepository;
        _keycloakUserService = keycloakUserService;
    }

    public async Task<UserDetailDto> HandleAsync(
        GetUserDetailsQuery query, CancellationToken ct = default)
    {
        var company = await _adminRepository.GetByIdAsync(query.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        var kcUser = await _keycloakUserService.GetUserByEmailAsync("client", company.Email.Value, ct);

        return new UserDetailDto(
            company.Id,
            company.RazaoSocial,
            company.Email.Value,
            company.Phone.Value,
            FormatCnpj(company.Cnpj?.Value),
            "PJ",
            default, // CreatedAt — not tracked on Company aggregate
            company.DeletedAt,
            kcUser is not null,
            true, // emailVerified — we set it to true on creation
            kcUser?.Id);
    }

    private static string? FormatCnpj(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return null;
        var digits = new string(cnpj.Where(char.IsDigit).ToArray());
        if (digits.Length != 14) return cnpj;
        return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}";
    }
}