using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: paginated list of employees with optional search, status and company filters (MGMT-01).
/// Admin sees ALL companies when CompanyId is null — bypasses HasQueryFilter.
/// </summary>
public sealed record GetPaginatedEmployeesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null,
    Guid? CompanyId = null)
    : IQuery<PaginatedResult<EmployeeSummaryDto>>;

public sealed class GetPaginatedEmployeesHandler
    : IQueryHandler<GetPaginatedEmployeesQuery, PaginatedResult<EmployeeSummaryDto>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICompanyRepository _companyRepository;

    public GetPaginatedEmployeesHandler(
        IEmployeeRepository employeeRepository,
        ICompanyRepository companyRepository)
    {
        _employeeRepository = employeeRepository;
        _companyRepository = companyRepository;
    }

    public async Task<PaginatedResult<EmployeeSummaryDto>> HandleAsync(
        GetPaginatedEmployeesQuery query, CancellationToken ct = default)
    {
        IReadOnlyList<Onboarding.Domain.Aggregates.EmployeeAggregate.Employee> employees;
        int totalCount;

        if (query.CompanyId.HasValue)
        {
            // Filtered by company — admin can narrow results
            (employees, totalCount) = await _employeeRepository.GetPagedByCompanyAsync(
                query.CompanyId.Value, query.Page, query.PageSize, query.Search, query.Status, ct);
        }
        else
        {
            // All companies — admin sees everything (bypasses HasQueryFilter)
            (employees, totalCount) = await _employeeRepository.GetPagedAllAsync(
                query.Page, query.PageSize, query.Search, query.Status, ct);
        }

        // Batch-load company names for the returned employee CompanyIds
        var companyIds = employees.Select(e => e.CompanyId).Distinct().ToList();
        var companies = new Dictionary<Guid, string>();
        foreach (var cid in companyIds)
        {
            var company = await _companyRepository.GetByIdAsync(cid, ct);
            if (company is not null)
                companies[cid] = company.RazaoSocial;
        }

        var dtos = employees.Select(e => new EmployeeSummaryDto(
            Id: e.Id,
            Nome: e.Nome,
            Cpf: e.Cpf?.Value ?? string.Empty,
            Email: e.Email.Value,
            Phone: e.Phone.Value,
            CompanyId: e.CompanyId,
            CompanyRazaoSocial: companies.GetValueOrDefault(e.CompanyId),
            AccessGroupId: e.AccessGroupId,
            AccessGroupName: null, // TODO: resolve from AccessGroup repository in future phase
            IsDeleted: e.IsDeleted,
            KeycloakUserId: e.KeycloakUserId)).ToList();

        return new PaginatedResult<EmployeeSummaryDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}