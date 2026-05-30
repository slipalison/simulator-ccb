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
    private readonly IAccessGroupRepository _accessGroupRepository;

    public GetPaginatedEmployeesHandler(
        IEmployeeRepository employeeRepository,
        ICompanyRepository companyRepository,
        IAccessGroupRepository accessGroupRepository)
    {
        _employeeRepository = employeeRepository;
        _companyRepository = companyRepository;
        _accessGroupRepository = accessGroupRepository;
    }

    public async Task<PaginatedResult<EmployeeSummaryDto>> HandleAsync(
        GetPaginatedEmployeesQuery query, CancellationToken ct = default)
    {
        IReadOnlyList<Onboarding.Domain.Aggregates.EmployeeAggregate.Employee> employees;
        int totalCount;

        if (query.CompanyId.HasValue)
        {
            (employees, totalCount) = await _employeeRepository.GetPagedByCompanyAsync(
                query.CompanyId.Value, query.Page, query.PageSize, query.Search, query.Status, ct);
        }
        else
        {
            (employees, totalCount) = await _employeeRepository.GetPagedAllAsync(
                query.Page, query.PageSize, query.Search, query.Status, ct);
        }

        // Batch-load companies and access groups in two single queries instead of N per-row lookups (PERF-03).
        var companyIds = employees.Select(e => e.CompanyId).Distinct();
        var companyList = await _companyRepository.GetByIdsAsync(companyIds, ct).ConfigureAwait(false);
        var companies = companyList.ToDictionary(c => c.Id, c => c.RazaoSocial);

        var accessGroupIds = employees.Select(e => e.AccessGroupId).Distinct();
        var accessGroupList = await _accessGroupRepository.GetByIdsAsync(accessGroupIds, ct).ConfigureAwait(false);
        var accessGroups = accessGroupList.ToDictionary(ag => ag.Id, ag => ag.Name);

        var dtos = employees.Select(e => new EmployeeSummaryDto(
            Id: e.Id,
            Nome: e.Nome,
            Cpf: e.Cpf?.Value ?? string.Empty,
            Email: e.Email.Value,
            Phone: e.Phone.Value,
            CompanyId: e.CompanyId,
            CompanyRazaoSocial: companies.GetValueOrDefault(e.CompanyId),
            AccessGroupId: e.AccessGroupId,
            AccessGroupName: accessGroups.GetValueOrDefault(e.AccessGroupId),
            IsDeleted: e.IsDeleted,
            KeycloakUserId: e.KeycloakUserId)).ToList();

        return new PaginatedResult<EmployeeSummaryDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}