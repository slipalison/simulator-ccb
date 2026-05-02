using Microsoft.Extensions.Logging;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: paginated list of companies with optional search and status filters (ADMIN-01).
/// </summary>
public sealed record GetPaginatedCompaniesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null)
    : IQuery<PaginatedResult<CompanySummaryDto>>;

/// <summary>
/// Handler: paginated list of companies with search and status filters (ADMIN-01).
/// </summary>
public sealed class GetPaginatedCompaniesHandler
    : IQueryHandler<GetPaginatedCompaniesQuery, PaginatedResult<CompanySummaryDto>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<GetPaginatedCompaniesHandler> _logger;

    public GetPaginatedCompaniesHandler(
        ICompanyRepository companyRepository,
        ILogger<GetPaginatedCompaniesHandler> logger)
    {
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<PaginatedResult<CompanySummaryDto>> HandleAsync(
        GetPaginatedCompaniesQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _companyRepository.GetPagedAsync(
            query.Page, query.PageSize, query.Search, query.Status, ct);

        var dtos = items.Select(MapToDto).ToList();

        return new PaginatedResult<CompanySummaryDto>(dtos, totalCount, query.Page, query.PageSize);
    }

    private static CompanySummaryDto MapToDto(Onboarding.Domain.Aggregates.CompanyAggregate.Company company) => new(
        Id: company.Id,
        RazaoSocial: company.RazaoSocial,
        Cnpj: company.Cnpj?.Value ?? string.Empty,
        Email: company.Email.Value,
        Phone: company.Phone.Value,
        IsDeleted: company.IsDeleted,
        KeycloakUserId: company.KeycloakUserId);
}