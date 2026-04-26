using Onboarding.Domain.Common;
using Perm = Onboarding.Domain.Aggregates.EmployeeAggregate.Permissions;

namespace Onboarding.Domain.Aggregates.EmployeeAggregate;

/// <summary>
/// AccessGroup entity — configurable group with permissions per Company (D-06, D-07).
/// Company admins manage their own groups. 3 default groups seeded on Company registration (D-08).
/// </summary>
public sealed class AccessGroup : Entity<Guid>
{
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = default!;
    public List<string> Permissions { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }

    private AccessGroup() { }

    /// <summary>
    /// Creates a new AccessGroup with the given permissions.
    /// </summary>
    public static AccessGroup Create(Guid companyId, string name, IEnumerable<string> permissions)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Access group name is required.", nameof(name));

        return new AccessGroup
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = name,
            Permissions = permissions.ToList(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates the 3 default access groups for a new Company (D-08).
    /// 1. admin-empresa — all 6 permissions
    /// 2. viewer — employees:read + audit:read
    /// 3. dashboard — dashboard:access
    /// </summary>
    public static IReadOnlyList<AccessGroup> CreateDefaultGroups(Guid companyId)
    {
        return
        [
            Create(companyId, "admin-empresa", Perm.All),
            Create(companyId, "viewer", [Perm.EmployeesRead, Perm.AuditRead]),
            Create(companyId, "dashboard", [Perm.DashboardAccess])
        ];
    }

    /// <summary>
    /// Replaces the permissions list. Validates each against the predefined list (T-37-04).
    /// </summary>
    public void UpdatePermissions(IEnumerable<string> permissions)
    {
        var permissionList = permissions.ToList();
        foreach (var perm in permissionList)
        {
            if (!Perm.All.Contains(perm))
                throw new ArgumentException($"Invalid permission: '{perm}'.", nameof(permissions));
        }

        Permissions = permissionList;
    }
}