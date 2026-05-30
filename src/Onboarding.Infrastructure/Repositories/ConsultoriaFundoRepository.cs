using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// Thin EF Core repository wrapper — tested via integration tests.
/// HasQueryFilter on ConsultoriaFundoConfiguration ensures company isolation (D-01).
/// IgnoreQueryFilters for uniqueness checks and direct lookups (D-12).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ConsultoriaFundoRepository : IConsultoriaFundoRepository
{
    private readonly AppDbContext _db;

    public ConsultoriaFundoRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(ConsultoriaFundo consultoria, CancellationToken ct = default)
    {
        await _db.ConsultoriasFundo.AddAsync(consultoria, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(ConsultoriaFundo consultoria, CancellationToken ct = default)
    {
        _db.ConsultoriasFundo.Update(consultoria);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ConsultoriaFundo?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.ConsultoriasFundo
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<bool> ExistsByCnpjAsync(string cnpj, Guid companyId, CancellationToken ct = default)
    {
        var cnpjVo = Cnpj.Create(cnpj);
        return await _db.ConsultoriasFundo
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Cnpj == cnpjVo && c.ClienteId == companyId, ct);
    }

    public async Task<(IReadOnlyList<ConsultoriaFundo> Items, int TotalCount)> GetPagedByCompanyAsync(
        Guid companyId, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var baseQuery = _db.ConsultoriasFundo
            .IgnoreQueryFilters()
            .Where(c => c.ClienteId == companyId)
            .AsNoTracking();

        IQueryable<ConsultoriaFundo> query;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            var digitsOnly = new string(normalized.Where(char.IsDigit).ToArray());

            // Name filter is translatable to SQL via ILike directly.
            var nameFilteredIds = await baseQuery
                .Where(c =>
                    EF.Functions.ILike(c.RazaoSocial, $"%{normalized}%") ||
                    (c.NomeFantasia != null && EF.Functions.ILike(c.NomeFantasia, $"%{normalized}%")))
                .Select(c => c.Id)
                .ToListAsync(ct);

            // CNPJ filter: EF Core 10 cannot translate HasConversion VO.Value in ILike expressions.
            // Materialize Id+Cnpj pairs for this company (tenant-scoped via baseQuery) and filter
            // client-side. The Select projects the Cnpj VO — EF applies HasConversion on load.
            HashSet<Guid> matchedIds = nameFilteredIds.ToHashSet();
            if (digitsOnly.Length > 0)
            {
                var cnpjProjection = await baseQuery
                    .Select(c => new { c.Id, c.Cnpj })
                    .ToListAsync(ct);

                foreach (var item in cnpjProjection)
                {
                    if (item.Cnpj.Value.Contains(digitsOnly, StringComparison.OrdinalIgnoreCase))
                        matchedIds.Add(item.Id);
                }
            }

            query = baseQuery.Where(c => matchedIds.Contains(c.Id));
        }
        else
        {
            query = baseQuery;
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.RazaoSocial)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.AsReadOnly(), totalCount);
    }
}