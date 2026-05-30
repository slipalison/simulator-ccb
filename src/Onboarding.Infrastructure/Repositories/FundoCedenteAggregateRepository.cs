using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for FundoCedenteAggregate — tested via integration tests.
/// Tenant scoping enforced by parent Fundo HasQueryFilter; no global filter here (D-5).
/// </summary>
public sealed class FundoCedenteAggregateRepository : IFundoCedenteAggregateRepository
{
    private readonly AppDbContext _db;

    public FundoCedenteAggregateRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(FundoCedenteAggregate association, CancellationToken ct = default)
    {
        await _db.RelFundoCedentes.AddAsync(association, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SaveAsync(FundoCedenteAggregate association, CancellationToken ct = default)
    {
        _db.RelFundoCedentes.Update(association);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<FundoCedenteAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.RelFundoCedentes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<bool> ExistsActiveAsync(Guid fundoId, Guid cedenteId, CancellationToken ct = default)
        => await _db.RelFundoCedentes
            .AnyAsync(r => r.FundoId == fundoId
                        && r.CedenteId == cedenteId
                        && r.Status == RelationshipStatus.ATIVO, ct)
            .ConfigureAwait(false);

    public async Task<(IReadOnlyList<FundoCedenteAggregate> Items, int TotalCount)> GetPagedByFundoAsync(
        Guid fundoId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.RelFundoCedentes
            .AsNoTracking()
            .Where(r => r.FundoId == fundoId)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, totalCount);
    }
}
