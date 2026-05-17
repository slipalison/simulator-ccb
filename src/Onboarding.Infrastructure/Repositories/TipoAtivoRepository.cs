using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// Thin EF Core repository wrapper — tested via integration tests.
/// Global scope — no HasQueryFilter, no company filter (D-03/TEN-03).
/// TipoAtivo is a shared CVM catalog accessible to all companies.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TipoAtivoRepository : ITipoAtivoRepository
{
    private readonly AppDbContext _db;

    public TipoAtivoRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(TipoAtivo tipoAtivo, CancellationToken ct = default)
    {
        await _db.TiposAtivo.AddAsync(tipoAtivo, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(TipoAtivo tipoAtivo, CancellationToken ct = default)
    {
        _db.TiposAtivo.Update(tipoAtivo);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<TipoAtivo?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.TiposAtivo
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<bool> ExistsByCodigoAsync(string codigo, CancellationToken ct = default)
        => await _db.TiposAtivo.AnyAsync(t => t.Codigo == codigo, ct);

    public async Task<(IReadOnlyList<TipoAtivo> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = _db.TiposAtivo.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();

            query = query.Where(t =>
                EF.Functions.ILike(t.Codigo, $"%{normalized}%") ||
                EF.Functions.ILike(t.Descricao, $"%{normalized}%"));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(t => t.OrdemExibicao)
            .ThenBy(t => t.Codigo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.AsReadOnly(), totalCount);
    }
}