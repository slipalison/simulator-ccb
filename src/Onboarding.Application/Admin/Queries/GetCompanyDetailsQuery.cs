using Microsoft.Extensions.Logging;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: detailed company data by ID (ADMIN-02).
/// </summary>
public sealed record GetCompanyDetailsQuery(Guid CompanyId)
    : IQuery<CompanySummaryDto>;

/// <summary>
/// Handler: get detailed company data by ID (ADMIN-02).
/// Throws KeyNotFoundException if company not found.
/// </summary>
public sealed class GetCompanyDetailsHandler
    : IQueryHandler<GetCompanyDetailsQuery, CompanySummaryDto>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ILogger<GetCompanyDetailsHandler> _logger;

    public GetCompanyDetailsHandler(
        ICompanyRepository companyRepository,
        ILogger<GetCompanyDetailsHandler> logger)
    {
        _companyRepository = companyRepository;
        _logger = logger;
    }

    public async Task<CompanySummaryDto> HandleAsync(
        GetCompanyDetailsQuery query, CancellationToken ct = default)
    {
        var company = await _companyRepository.GetByIdAsync(query.CompanyId, ct);
        if (company is null)
            throw new KeyNotFoundException($"Company with ID '{query.CompanyId}' not found.");

        return new CompanySummaryDto(
            Id: company.Id,
            RazaoSocial: company.RazaoSocial,
            Cnpj: company.Cnpj?.Value ?? string.Empty,
            Email: company.Email.Value,
            Phone: company.Phone.Value,
            IsDeleted: company.IsDeleted,
            KeycloakUserId: company.KeycloakUserId);
    }
}