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

    public GetEmployeeDetailsHandler(
        IEmployeeRepository employeeRepository,
        ICompanyRepository companyRepository)
    {
        _employeeRepository = employeeRepository;
        _companyRepository = companyRepository;
    }

    public async Task<EmployeeSummaryDto> HandleAsync(
        GetEmployeeDetailsQuery query, CancellationToken ct = default)
    {
        // Admin bypasses HasQueryFilter — can see employees from any company
        var employee = await _employeeRepository.GetByIdIgnoreFilterAsync(query.EmployeeId, ct)
            ?? throw new KeyNotFoundException($"Employee with ID {query.EmployeeId} not found.");

        // Resolve company name
        var company = await _companyRepository.GetByIdAsync(employee.CompanyId, ct);
        var companyRazaoSocial = company?.RazaoSocial;

        return new EmployeeSummaryDto(
            Id: employee.Id,
            Nome: employee.Nome,
            Cpf: employee.Cpf?.Value ?? string.Empty,
            Email: employee.Email.Value,
            Phone: employee.Phone.Value,
            CompanyId: employee.CompanyId,
            CompanyRazaoSocial: companyRazaoSocial,
            AccessGroupId: employee.AccessGroupId,
            AccessGroupName: null, // TODO: resolve in future phase
            IsDeleted: employee.IsDeleted,
            KeycloakUserId: employee.KeycloakUserId);
    }
}