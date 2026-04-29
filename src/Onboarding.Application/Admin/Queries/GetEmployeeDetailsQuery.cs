using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Common;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Admin.Queries;

/// <summary>
/// Query: detailed employee data for admin (MGMT-02).
/// Bypasses HasQueryFilter — admin can see employees from any company.
/// </summary>
public sealed record GetEmployeeDetailsQuery(Guid EmployeeId)
    : IQuery<EmployeeSummaryDto>;

public sealed class GetEmployeeDetailsHandler
    : IQueryHandler<GetEmployeeDetailsQuery, EmployeeSummaryDto>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IAccessGroupRepository _accessGroupRepository;

    public GetEmployeeDetailsHandler(
        IEmployeeRepository employeeRepository,
        ICompanyRepository companyRepository,
        IAccessGroupRepository accessGroupRepository)
    {
        _employeeRepository = employeeRepository;
        _companyRepository = companyRepository;
        _accessGroupRepository = accessGroupRepository;
    }

    public async Task<EmployeeSummaryDto> HandleAsync(
        GetEmployeeDetailsQuery query, CancellationToken ct = default)
    {
        var employee = await _employeeRepository.GetByIdIgnoreFilterAsync(query.EmployeeId, ct)
            ?? throw new KeyNotFoundException($"Employee with ID {query.EmployeeId} not found.");

        var company = await _companyRepository.GetByIdAsync(employee.CompanyId, ct);
        var accessGroup = await _accessGroupRepository.GetByIdAsync(employee.AccessGroupId, ct);

        return new EmployeeSummaryDto(
            Id: employee.Id,
            Nome: employee.Nome,
            Cpf: employee.Cpf?.Value ?? string.Empty,
            Email: employee.Email.Value,
            Phone: employee.Phone.Value,
            CompanyId: employee.CompanyId,
            CompanyRazaoSocial: company?.RazaoSocial,
            AccessGroupId: employee.AccessGroupId,
            AccessGroupName: accessGroup?.Name,
            IsDeleted: employee.IsDeleted,
            KeycloakUserId: employee.KeycloakUserId);
    }
}