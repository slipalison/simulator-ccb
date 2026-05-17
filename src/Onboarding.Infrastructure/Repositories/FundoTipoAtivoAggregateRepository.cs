using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Aggregates.FundoTipoAtivoAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for FundoTipoAtivoAggregate — tested via integration tests.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class FundoTipoAtivoAggregateRepository : IFundoTipoAtivoAggregateRepository
{
    private readonly AppDbContext _db;

    public FundoTipoAtivoAggregateRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(FundoTipoAtivoAggregate association, CancellationToken ct = default)
    {
        await _db.RelFundoTiposAtivo.AddAsync(association, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SaveAsync(FundoTipoAtivoAggregate association, CancellationToken ct = default)
    {
        _db.RelFundoTiposAtivo.Update(association);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<FundoTipoAtivoAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.RelFundoTiposAtivo
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<bool> ExistsActiveAsync(Guid fundoId, Guid tipoAtivoId, CancellationToken ct = default)
        => await _db.RelFundoTiposAtivo
            .AnyAsync(r => r.FundoId == fundoId
                        && r.TipoAtivoId == tipoAtivoId
                        && r.Status == RelationshipStatus.ATIVO, ct)
            .ConfigureAwait(false);

    public async Task<(IReadOnlyList<FundoTipoAtivoAggregate> Items, int TotalCount)> GetPagedByFundoAsync(
        Guid fundoId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.RelFundoTiposAtivo
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
