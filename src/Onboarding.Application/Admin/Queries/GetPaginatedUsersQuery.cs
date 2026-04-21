using Microsoft.Extensions.Logging;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: paginated list of users with optional search and status filters (ADMIN-01).
/// </summary>
public sealed record GetPaginatedUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null)
    : IQuery<PaginatedResult<UserSummaryDto>>;

public sealed class GetPaginatedUsersHandler
    : IQueryHandler<GetPaginatedUsersQuery, PaginatedResult<UserSummaryDto>>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly ILogger<GetPaginatedUsersHandler> _logger;

    public GetPaginatedUsersHandler(
        IAdminRepository adminRepository,
        IKeycloakUserService keycloakUserService,
        ILogger<GetPaginatedUsersHandler> logger)
    {
        _adminRepository = adminRepository;
        _keycloakUserService = keycloakUserService;
        _logger = logger;
    }

    public async Task<PaginatedResult<UserSummaryDto>> HandleAsync(
        GetPaginatedUsersQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _adminRepository.GetPagedAsync(
            query.Page, query.PageSize, query.Search, query.Status, ct);

        var dtoItems = new List<UserSummaryDto>(items.Count);

        foreach (var client in items)
        {
            bool enabled = false;

            try
            {
                var kcUser = await _keycloakUserService.GetUserByEmailAsync("client", client.Email.Value, ct);
                enabled = kcUser is not null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get Keycloak status for user {Email}", client.Email.Value);
            }

            var document = client.Type.ToString() == "PessoaFisica"
                ? FormatCpf(client.Cpf?.Value)
                : FormatCnpj(client.Cnpj?.Value);

            dtoItems.Add(new UserSummaryDto(
                client.Id,
                client.Name,
                client.Email.Value,
                document,
                client.Type.ToString() == "PessoaFisica" ? "PF" : "PJ",
                enabled,
                client.DeletedAt));
        }

        return new PaginatedResult<UserSummaryDto>(
            dtoItems.AsReadOnly(),
            totalCount,
            query.Page,
            query.PageSize);
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
