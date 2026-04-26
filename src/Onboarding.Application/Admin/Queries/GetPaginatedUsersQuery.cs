using Microsoft.Extensions.Logging;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: paginated list of companies with optional search and status filters (ADMIN-01).
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

        foreach (var company in items)
        {
            bool enabled = false;

            try
            {
                var kcUser = await _keycloakUserService.GetUserByEmailAsync("client", company.Email.Value, ct);
                enabled = kcUser is not null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get Keycloak status for company {Email}", company.Email.Value);
            }

            dtoItems.Add(new UserSummaryDto(
                company.Id,
                company.RazaoSocial,
                company.Email.Value,
                FormatCnpj(company.Cnpj?.Value),
                "PJ",
                enabled,
                company.DeletedAt));
        }

        return new PaginatedResult<UserSummaryDto>(
            dtoItems.AsReadOnly(),
            totalCount,
            query.Page,
            query.PageSize);
    }

    private static string? FormatCnpj(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return null;
        var digits = new string(cnpj.Where(char.IsDigit).ToArray());
        if (digits.Length != 14) return cnpj;
        return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}";
    }
}