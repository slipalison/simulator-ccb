using Onboarding.Application.Common;
using Onboarding.Application.Companies.DTOs;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Companies.Queries;

public sealed class GetCompanyEmployeesQueryHandler
    : IQueryHandler<GetCompanyEmployeesQuery, PaginatedResult<EmployeeListItemDto>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly IKeycloakUserService _keycloakUserService;

    public GetCompanyEmployeesQueryHandler(
        IEmployeeRepository employeeRepository,
        IAccessGroupRepository accessGroupRepository,
        IKeycloakUserService keycloakUserService)
    {
        _employeeRepository = employeeRepository;
        _accessGroupRepository = accessGroupRepository;
        _keycloakUserService = keycloakUserService;
    }

    public async Task<PaginatedResult<EmployeeListItemDto>> HandleAsync(
        GetCompanyEmployeesQuery query, CancellationToken ct = default)
    {
        var (employees, totalCount) = await _employeeRepository.GetPagedByCompanyAsync(
            query.CompanyId, query.Page, query.PageSize, query.Search, query.Status, ct);

        var dtos = new List<EmployeeListItemDto>();
        foreach (var employee in employees)
        {
            var accessGroup = await _accessGroupRepository.GetByIdAsync(employee.AccessGroupId, ct);

            bool keycloakEnabled = true;
            if (!string.IsNullOrEmpty(employee.KeycloakUserId))
            {
                var userDetails = await _keycloakUserService.GetUserByIdAsync("client", employee.KeycloakUserId, ct);
                keycloakEnabled = userDetails?.Enabled ?? false;
            }

            dtos.Add(new EmployeeListItemDto(
                Id: employee.Id,
                Nome: employee.Nome,
                Cpf: employee.Cpf?.Value,
                Email: employee.Email.Value,
                Phone: employee.Phone.Value,
                AccessGroupId: employee.AccessGroupId,
                AccessGroupName: accessGroup?.Name ?? string.Empty,
                IsDeleted: employee.IsDeleted,
                KeycloakEnabled: keycloakEnabled));
        }

        return new PaginatedResult<EmployeeListItemDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}