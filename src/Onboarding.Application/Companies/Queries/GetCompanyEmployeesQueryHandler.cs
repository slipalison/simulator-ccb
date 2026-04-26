using Onboarding.Application.Common;
using Onboarding.Application.Companies.DTOs;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Companies.Queries;

/// <summary>
/// Handler: paginated employee listing scoped to company (MGMT-02).
/// Maps Employee to EmployeeListItemDto with AccessGroup name resolution.
/// </summary>
public sealed class GetCompanyEmployeesQueryHandler
    : IQueryHandler<GetCompanyEmployeesQuery, PaginatedResult<EmployeeListItemDto>>
{
    private readonly IEmployeeRepository _employeeRepository;

    public GetCompanyEmployeesQueryHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<PaginatedResult<EmployeeListItemDto>> HandleAsync(
        GetCompanyEmployeesQuery query, CancellationToken ct = default)
    {
        var (employees, totalCount) = await _employeeRepository.GetPagedByCompanyAsync(
            query.CompanyId, query.Page, query.PageSize, query.Search, query.Status, ct);

        var dtos = employees.Select(MapToDto).ToList();

        return new PaginatedResult<EmployeeListItemDto>(dtos, totalCount, query.Page, query.PageSize);
    }

    private static EmployeeListItemDto MapToDto(Employee employee) => new(
        Id: employee.Id,
        Nome: employee.Nome,
        Cpf: employee.Cpf?.Value,
        Email: employee.Email.Value,
        Phone: employee.Phone.Value,
        AccessGroupName: string.Empty, // Populated by controller/join query if needed
        IsDeleted: employee.IsDeleted);
}