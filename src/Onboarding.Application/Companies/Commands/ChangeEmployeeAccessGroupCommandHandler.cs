using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;

namespace Onboarding.Application.Companies.Commands;

/// <summary>
/// Handler: changes employee's access group — verifies new group belongs to same company (T-38-11).
/// </summary>
public sealed class ChangeEmployeeAccessGroupCommandHandler : ICommandHandler<ChangeEmployeeAccessGroupCommand, Unit>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly IAuditService _auditService;

    public ChangeEmployeeAccessGroupCommandHandler(
        IEmployeeRepository employeeRepository,
        IAccessGroupRepository accessGroupRepository,
        IAuditService auditService)
    {
        _employeeRepository = employeeRepository;
        _accessGroupRepository = accessGroupRepository;
        _auditService = auditService;
    }

    public async Task<Unit> HandleAsync(ChangeEmployeeAccessGroupCommand command, CancellationToken ct = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(command.EmployeeId, ct)
            ?? throw new KeyNotFoundException($"Employee with ID {command.EmployeeId} not found.");

        // Company isolation (T-38-08)
        if (employee.CompanyId != command.CompanyId)
            throw new InvalidOperationException("Employee does not belong to the specified company.");

        // Verify new access group belongs to same company (T-38-11)
        var newGroup = await _accessGroupRepository.GetByIdAsync(command.NewAccessGroupId, ct)
            ?? throw new KeyNotFoundException($"Access group with ID {command.NewAccessGroupId} not found.");

        if (newGroup.CompanyId != command.CompanyId)
            throw new InvalidOperationException("Access group does not belong to the specified company.");

        // Change access group
        employee.SetAccessGroup(command.NewAccessGroupId);
        await _employeeRepository.SaveAsync(employee, ct);

        // Audit (T-38-10)
        await _auditService.RecordAsync(
            actorSub: command.ActorSub,
            actorEmail: command.ActorEmail,
            action: ActionType.AccessGroupChanged,
            targetUserId: employee.Id,
            targetUserName: employee.Nome,
            details: $"Access group changed to {command.NewAccessGroupId}",
            ipAddress: command.IpAddress,
            ct: ct);

        return Unit.Value;
    }
}