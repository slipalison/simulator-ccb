using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _db;

    public AuditLogRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(AuditLog log, CancellationToken ct = default)
        => await _db.AuditLogs.AddAsync(log, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
