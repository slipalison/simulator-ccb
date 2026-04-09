using Onboarding.Domain.Aggregates.Audit;

namespace Onboarding.Domain.Repositories;

/// <summary>
/// Audit log persistence — defined in Domain, implemented in Infrastructure (Plan 02).
/// </summary>
public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
