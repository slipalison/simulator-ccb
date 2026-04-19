using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: detailed user data including Keycloak status (ADMIN-02).
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
        var client = await _adminRepository.GetByIdAsync(query.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        var kcUser = await _keycloakUserService.GetUserByEmailAsync("client", client.Email.Value, ct);

        var document = client.Type.ToString() == "PessoaFisica"
            ? FormatCpf(client.Cpf?.Value)
            : FormatCnpj(client.Cnpj?.Value);

        return new UserDetailDto(
            client.Id,
            client.Name,
            client.Email.Value,
            client.Phone.Value,
            document,
            client.Type.ToString() == "PessoaFisica" ? "PF" : "PJ",
            client.RazaoSocial,
            default, // CreatedAt — not tracked on Client aggregate
            client.DeletedAt,
            kcUser is not null,
            true, // emailVerified — we set it to true on creation
            kcUser?.Id);
    }

    private static string? FormatCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return null;
        var digits = new string(cpf.Where(char.IsDigit).ToArray());
        if (digits.Length != 11) return cpf;
        return $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}";
    }

    private static string? FormatCnpj(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return null;
        var digits = new string(cnpj.Where(char.IsDigit).ToArray());
        if (digits.Length != 14) return cnpj;
        return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}";
    }
}
