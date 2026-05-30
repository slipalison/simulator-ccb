using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.CedenteTipoAtivoAggregate;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for CedenteTipoAtivoAggregate — tested via integration tests.
/// </summary>
public sealed class CedenteTipoAtivoAggregateRepository : ICedenteTipoAtivoAggregateRepository
{
    private readonly AppDbContext _db;

    public CedenteTipoAtivoAggregateRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(CedenteTipoAtivoAggregate association, CancellationToken ct = default)
    {
        await _db.RelCedenteTiposAtivo.AddAsync(association, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SaveAsync(CedenteTipoAtivoAggregate association, CancellationToken ct = default)
    {
        _db.RelCedenteTiposAtivo.Update(association);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<CedenteTipoAtivoAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.RelCedenteTiposAtivo
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<bool> ExistsActiveAsync(Guid cedenteId, Guid tipoAtivoId, CancellationToken ct = default)
        => await _db.RelCedenteTiposAtivo
            .AnyAsync(r => r.CedenteId == cedenteId
                        && r.TipoAtivoId == tipoAtivoId
                        && r.Status == RelationshipStatus.ATIVO, ct)
            .ConfigureAwait(false);

    public async Task<(IReadOnlyList<CedenteTipoAtivoAggregate> Items, int TotalCount)> GetPagedByCedenteAsync(
        Guid cedenteId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.RelCedenteTiposAtivo
            .AsNoTracking()
            .Where(r => r.CedenteId == cedenteId)
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
