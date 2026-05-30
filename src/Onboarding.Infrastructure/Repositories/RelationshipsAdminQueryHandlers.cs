using Microsoft.EntityFrameworkCore;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Queries.Admin;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// Cross-company admin query handlers for relationship aggregates (D-8, D-12).
/// All handlers use IgnoreQueryFilters() — no tenant isolation applied.
/// SECURITY: These handlers MUST only be consumed by AdminFundosController
/// which requires Policy = CrossCompanyAccess (BearerBackoffice).
/// </summary>
public sealed class ListAdminFundoCedenteQueryHandler
    : IQueryHandler<ListAdminFundoCedenteQuery, PaginatedResult<AdminRelFundoCedenteDto>>
{
    private readonly AppDbContext _db;

    public ListAdminFundoCedenteQueryHandler(AppDbContext db) => _db = db;

    public async Task<PaginatedResult<AdminRelFundoCedenteDto>> HandleAsync(
        ListAdminFundoCedenteQuery query, CancellationToken ct = default)
    {
        // IgnoreQueryFilters — cross-company admin read (D-8, D-12).
        // Join rel_fundo_cedente → Fundos → Companies to expose EmpresaNome + ClienteId.
        var baseQuery = _db.RelFundoCedentes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Join(
                _db.Fundos.AsNoTracking().IgnoreQueryFilters(),
                rel => rel.FundoId,
                f => f.Id,
                (rel, f) => new { Rel = rel, Fundo = f })
            .Join(
                _db.Companies.AsNoTracking(),
                x => x.Fundo.ClienteId,
                c => c.Id,
                (x, c) => new { x.Rel, x.Fundo, EmpresaNome = c.RazaoSocial })
            .AsQueryable();

        if (query.CompanyId.HasValue)
            baseQuery = baseQuery.Where(x => x.Fundo.ClienteId == query.CompanyId.Value);

        var totalCount = await baseQuery.CountAsync(ct).ConfigureAwait(false);

        var items = await baseQuery
            .OrderBy(x => x.EmpresaNome)
            .ThenBy(x => x.Fundo.Nome)
            .ThenBy(x => x.Rel.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AdminRelFundoCedenteDto(
                x.Rel.Id,
                x.Fundo.ClienteId,
                x.EmpresaNome,
                x.Rel.FundoId,
                x.Fundo.Nome,
                x.Rel.CedenteId,
                x.Rel.Limite.Percentual,
                x.Rel.Limite.Valor,
                x.Rel.Janela.DataInicio,
                x.Rel.Janela.DataFim,
                x.Rel.Status,
                x.Rel.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PaginatedResult<AdminRelFundoCedenteDto>(items, totalCount, query.Page, query.PageSize);
    }
}

/// <summary>
/// Cross-company admin query handler for CedenteTipoAtivo relationship listing (D-8, D-12).
/// </summary>
public sealed class ListAdminCedenteTipoAtivoQueryHandler
    : IQueryHandler<ListAdminCedenteTipoAtivoQuery, PaginatedResult<AdminRelCedenteTipoAtivoDto>>
{
    private readonly AppDbContext _db;

    public ListAdminCedenteTipoAtivoQueryHandler(AppDbContext db) => _db = db;

    public async Task<PaginatedResult<AdminRelCedenteTipoAtivoDto>> HandleAsync(
        ListAdminCedenteTipoAtivoQuery query, CancellationToken ct = default)
    {
        // IgnoreQueryFilters — cross-company admin read (D-8, D-12).
        // Join rel_cedente_tipo_ativo → Cedentes → Companies.
        var baseQuery = _db.RelCedenteTiposAtivo
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Join(
                _db.Cedentes.AsNoTracking().IgnoreQueryFilters(),
                rel => rel.CedenteId,
                c => c.Id,
                (rel, c) => new { Rel = rel, Cedente = c })
            .Join(
                _db.Companies.AsNoTracking(),
                x => x.Cedente.ClienteId,
                co => co.Id,
                (x, co) => new { x.Rel, x.Cedente, EmpresaNome = co.RazaoSocial })
            .AsQueryable();

        if (query.CompanyId.HasValue)
            baseQuery = baseQuery.Where(x => x.Cedente.ClienteId == query.CompanyId.Value);

        var totalCount = await baseQuery.CountAsync(ct).ConfigureAwait(false);

        var items = await baseQuery
            .OrderBy(x => x.EmpresaNome)
            .ThenBy(x => x.Rel.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AdminRelCedenteTipoAtivoDto(
                x.Rel.Id,
                x.Cedente.ClienteId,
                x.EmpresaNome,
                x.Rel.CedenteId,
                x.Rel.TipoAtivoId,
                x.Rel.Limite.Percentual,
                x.Rel.Limite.Valor,
                x.Rel.Janela.DataInicio,
                x.Rel.Janela.DataFim,
                x.Rel.Status,
                x.Rel.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PaginatedResult<AdminRelCedenteTipoAtivoDto>(items, totalCount, query.Page, query.PageSize);
    }
}

/// <summary>
/// Cross-company admin query handler for FundoTipoAtivo relationship listing (D-8, D-12).
/// </summary>
public sealed class ListAdminFundoTipoAtivoQueryHandler
    : IQueryHandler<ListAdminFundoTipoAtivoQuery, PaginatedResult<AdminRelFundoTipoAtivoDto>>
{
    private readonly AppDbContext _db;

    public ListAdminFundoTipoAtivoQueryHandler(AppDbContext db) => _db = db;

    public async Task<PaginatedResult<AdminRelFundoTipoAtivoDto>> HandleAsync(
        ListAdminFundoTipoAtivoQuery query, CancellationToken ct = default)
    {
        // IgnoreQueryFilters — cross-company admin read (D-8, D-12).
        // Join rel_fundo_tipo_ativo → Fundos → Companies.
        var baseQuery = _db.RelFundoTiposAtivo
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Join(
                _db.Fundos.AsNoTracking().IgnoreQueryFilters(),
                rel => rel.FundoId,
                f => f.Id,
                (rel, f) => new { Rel = rel, Fundo = f })
            .Join(
                _db.Companies.AsNoTracking(),
                x => x.Fundo.ClienteId,
                c => c.Id,
                (x, c) => new { x.Rel, x.Fundo, EmpresaNome = c.RazaoSocial })
            .AsQueryable();

        if (query.CompanyId.HasValue)
            baseQuery = baseQuery.Where(x => x.Fundo.ClienteId == query.CompanyId.Value);

        var totalCount = await baseQuery.CountAsync(ct).ConfigureAwait(false);

        var items = await baseQuery
            .OrderBy(x => x.EmpresaNome)
            .ThenBy(x => x.Fundo.Nome)
            .ThenBy(x => x.Rel.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AdminRelFundoTipoAtivoDto(
                x.Rel.Id,
                x.Fundo.ClienteId,
                x.EmpresaNome,
                x.Rel.FundoId,
                x.Fundo.Nome,
                x.Rel.TipoAtivoId,
                x.Rel.Limite.Percentual,
                x.Rel.Limite.Valor,
                x.Rel.Janela.DataInicio,
                x.Rel.Janela.DataFim,
                x.Rel.Status,
                x.Rel.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PaginatedResult<AdminRelFundoTipoAtivoDto>(items, totalCount, query.Page, query.PageSize);
    }
}
