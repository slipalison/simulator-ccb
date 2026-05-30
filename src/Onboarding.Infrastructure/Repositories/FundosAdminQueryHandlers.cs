using Microsoft.EntityFrameworkCore;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Queries.Admin;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// Cross-company admin query handlers for the Fundos module (D-8, D-12).
/// All handlers use IgnoreQueryFilters() — no tenant isolation applied.
/// Each query joins with Companies table to expose EmpresaNome per row.
/// SECURITY: These handlers MUST only be consumed by AdminFundosController
/// which requires Policy = CrossCompanyAccess (BearerBackoffice).
/// </summary>
public sealed class ListAdminFundoQueryHandler
    : IQueryHandler<ListAdminFundoQuery, PaginatedResult<AdminFundoDto>>
{
    private readonly AppDbContext _db;

    public ListAdminFundoQueryHandler(AppDbContext db) => _db = db;

    public async Task<PaginatedResult<AdminFundoDto>> HandleAsync(
        ListAdminFundoQuery query, CancellationToken ct = default)
    {
        // IgnoreQueryFilters — cross-company admin read (D-8, D-12).
        // cnpj is a value-converter column — opaque to LINQ (.Value cannot be translated at runtime).
        // When search is present, use FromSqlInterpolated on fundos first (parameterized, no injection),
        // then Join with Companies. IgnoreQueryFilters() suppresses the HasQueryFilter on the FromSql result.
        IQueryable<Fundo> fundoBase;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = $"%{query.Search.Trim()}%";
            var digits = new string(query.Search.Where(char.IsDigit).ToArray());
            var digitsLike = $"%{digits}%";
            var hasDigits = digits.Length > 0;
            fundoBase = _db.Fundos
                .FromSqlInterpolated($@"
                    SELECT * FROM fundos
                    WHERE nome ILIKE {like}
                       OR ({hasDigits} AND cnpj ILIKE {digitsLike})")
                .IgnoreQueryFilters()
                .AsNoTracking();
        }
        else
        {
            fundoBase = _db.Fundos.IgnoreQueryFilters().AsNoTracking();
        }

        var baseQuery = fundoBase
            .Join(
                _db.Companies.AsNoTracking(),
                f => f.ClienteId,
                c => c.Id,
                (f, c) => new { Fundo = f, EmpresaNome = c.RazaoSocial })
            .AsQueryable();

        if (query.CompanyId.HasValue)
            baseQuery = baseQuery.Where(x => x.Fundo.ClienteId == query.CompanyId.Value);

        var totalCount = await baseQuery.CountAsync(ct).ConfigureAwait(false);

        var items = await baseQuery
            .OrderBy(x => x.EmpresaNome)
            .ThenBy(x => x.Fundo.Nome)
            .ThenBy(x => x.Fundo.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AdminFundoDto(
                x.Fundo.Id,
                x.Fundo.ClienteId,
                x.EmpresaNome,
                x.Fundo.Nome,
                x.Fundo.Cnpj.Value,
                x.Fundo.ConsultoriaFundoId,
                x.Fundo.CustodianteId,
                x.Fundo.TipoFundo,
                x.Fundo.ClasseAnbima,
                x.Fundo.Segmento,
                x.Fundo.DataConstituicao,
                x.Fundo.Status,
                x.Fundo.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PaginatedResult<AdminFundoDto>(items, totalCount, query.Page, query.PageSize);
    }
}

/// <summary>
/// Cross-company admin query handler for ConsultoriaFundo listing (D-8, D-12).
/// </summary>
public sealed class ListAdminConsultoriaQueryHandler
    : IQueryHandler<ListAdminConsultoriaQuery, PaginatedResult<AdminConsultoriaFundoDto>>
{
    private readonly AppDbContext _db;

    public ListAdminConsultoriaQueryHandler(AppDbContext db) => _db = db;

    public async Task<PaginatedResult<AdminConsultoriaFundoDto>> HandleAsync(
        ListAdminConsultoriaQuery query, CancellationToken ct = default)
    {
        // cnpj is a value-converter column — opaque to LINQ (.Value cannot be translated at runtime).
        // When search is present, use FromSqlInterpolated on consultoria_fundos first (parameterized,
        // no injection), then Join with Companies. IgnoreQueryFilters() suppresses HasQueryFilter.
        IQueryable<ConsultoriaFundo> consultoriaBase;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = $"%{query.Search.Trim()}%";
            var digits = new string(query.Search.Where(char.IsDigit).ToArray());
            var digitsLike = $"%{digits}%";
            var hasDigits = digits.Length > 0;
            consultoriaBase = _db.ConsultoriasFundo
                .FromSqlInterpolated($@"
                    SELECT * FROM consultoria_fundos
                    WHERE razao_social ILIKE {like}
                       OR (nome_fantasia IS NOT NULL AND nome_fantasia ILIKE {like})
                       OR ({hasDigits} AND cnpj ILIKE {digitsLike})")
                .IgnoreQueryFilters()
                .AsNoTracking();
        }
        else
        {
            consultoriaBase = _db.ConsultoriasFundo.IgnoreQueryFilters().AsNoTracking();
        }

        var baseQuery = consultoriaBase
            .Join(
                _db.Companies.AsNoTracking(),
                c => c.ClienteId,
                co => co.Id,
                (c, co) => new { Consultoria = c, EmpresaNome = co.RazaoSocial })
            .AsQueryable();

        if (query.CompanyId.HasValue)
            baseQuery = baseQuery.Where(x => x.Consultoria.ClienteId == query.CompanyId.Value);

        var totalCount = await baseQuery.CountAsync(ct).ConfigureAwait(false);

        var items = await baseQuery
            .OrderBy(x => x.EmpresaNome)
            .ThenBy(x => x.Consultoria.RazaoSocial)
            .ThenBy(x => x.Consultoria.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AdminConsultoriaFundoDto(
                x.Consultoria.Id,
                x.Consultoria.ClienteId,
                x.EmpresaNome,
                x.Consultoria.RazaoSocial,
                x.Consultoria.NomeFantasia,
                x.Consultoria.Cnpj.Value,
                x.Consultoria.Email == null ? null : x.Consultoria.Email.Value,
                x.Consultoria.Telefone == null ? null : x.Consultoria.Telefone.Value,
                x.Consultoria.Status,
                x.Consultoria.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PaginatedResult<AdminConsultoriaFundoDto>(items, totalCount, query.Page, query.PageSize);
    }
}

/// <summary>
/// Cross-company admin query handler for Custodiante listing (D-8, D-12).
/// </summary>
public sealed class ListAdminCustodianteQueryHandler
    : IQueryHandler<ListAdminCustodianteQuery, PaginatedResult<AdminCustodianteDto>>
{
    private readonly AppDbContext _db;

    public ListAdminCustodianteQueryHandler(AppDbContext db) => _db = db;

    public async Task<PaginatedResult<AdminCustodianteDto>> HandleAsync(
        ListAdminCustodianteQuery query, CancellationToken ct = default)
    {
        // cnpj is a value-converter column — opaque to LINQ (.Value cannot be translated at runtime).
        // When search is present, use FromSqlInterpolated on custodiantes first (parameterized,
        // no injection), then Join with Companies. IgnoreQueryFilters() suppresses HasQueryFilter.
        IQueryable<Custodiante> custodianteBase;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = $"%{query.Search.Trim()}%";
            var digits = new string(query.Search.Where(char.IsDigit).ToArray());
            var digitsLike = $"%{digits}%";
            var hasDigits = digits.Length > 0;
            custodianteBase = _db.Custodiantes
                .FromSqlInterpolated($@"
                    SELECT * FROM custodiantes
                    WHERE razao_social ILIKE {like}
                       OR ({hasDigits} AND cnpj ILIKE {digitsLike})")
                .IgnoreQueryFilters()
                .AsNoTracking();
        }
        else
        {
            custodianteBase = _db.Custodiantes.IgnoreQueryFilters().AsNoTracking();
        }

        var baseQuery = custodianteBase
            .Join(
                _db.Companies.AsNoTracking(),
                cu => cu.ClienteId,
                co => co.Id,
                (cu, co) => new { Custodiante = cu, EmpresaNome = co.RazaoSocial })
            .AsQueryable();

        if (query.CompanyId.HasValue)
            baseQuery = baseQuery.Where(x => x.Custodiante.ClienteId == query.CompanyId.Value);

        var totalCount = await baseQuery.CountAsync(ct).ConfigureAwait(false);

        var items = await baseQuery
            .OrderBy(x => x.EmpresaNome)
            .ThenBy(x => x.Custodiante.RazaoSocial)
            .ThenBy(x => x.Custodiante.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AdminCustodianteDto(
                x.Custodiante.Id,
                x.Custodiante.ClienteId,
                x.EmpresaNome,
                x.Custodiante.RazaoSocial,
                x.Custodiante.CodigoInterno,
                x.Custodiante.Cnpj.Value,
                x.Custodiante.Email == null ? null : x.Custodiante.Email.Value,
                x.Custodiante.Telefone == null ? null : x.Custodiante.Telefone.Value,
                x.Custodiante.Status,
                x.Custodiante.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PaginatedResult<AdminCustodianteDto>(items, totalCount, query.Page, query.PageSize);
    }
}

/// <summary>
/// Cross-company admin query handler for Cedente listing (D-8, D-12).
/// Cedente document type (PF/PJ) is read from shadow property "DocumentoTipo" (D-09/CR-03).
/// The admin view shows the raw document value (CPF or CNPJ string) and CedenteTipo enum.
/// </summary>
public sealed class ListAdminCedenteQueryHandler
    : IQueryHandler<ListAdminCedenteQuery, PaginatedResult<AdminCedenteDto>>
{
    private readonly AppDbContext _db;

    public ListAdminCedenteQueryHandler(AppDbContext db) => _db = db;

    public async Task<PaginatedResult<AdminCedenteDto>> HandleAsync(
        ListAdminCedenteQuery query, CancellationToken ct = default)
    {
        // cpf / cnpj_cedente are shadow-property columns (D-09/CR-03).
        // EF.Property<string> after a Join produces an anonymous type where shadow property
        // access is unreliable. Use FromSqlInterpolated on cedentes directly (parameterized,
        // no injection), then Join with Companies. IgnoreQueryFilters() suppresses HasQueryFilter.
        IQueryable<Cedente> cedenteBase;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = $"%{query.Search.Trim()}%";
            var digits = new string(query.Search.Where(char.IsDigit).ToArray());
            var digitsLike = $"%{digits}%";
            var hasDigits = digits.Length > 0;
            cedenteBase = _db.Cedentes
                .FromSqlInterpolated($@"
                    SELECT * FROM cedentes
                    WHERE nome ILIKE {like}
                       OR ({hasDigits} AND cpf ILIKE {digitsLike})
                       OR ({hasDigits} AND cnpj_cedente ILIKE {digitsLike})")
                .IgnoreQueryFilters()
                .AsNoTracking();
        }
        else
        {
            cedenteBase = _db.Cedentes.IgnoreQueryFilters().AsNoTracking();
        }

        var baseQuery = cedenteBase
            .Join(
                _db.Companies.AsNoTracking(),
                ce => ce.ClienteId,
                co => co.Id,
                (ce, co) => new { Cedente = ce, EmpresaNome = co.RazaoSocial })
            .AsQueryable();

        if (query.CompanyId.HasValue)
            baseQuery = baseQuery.Where(x => x.Cedente.ClienteId == query.CompanyId.Value);

        var totalCount = await baseQuery.CountAsync(ct).ConfigureAwait(false);

        var items = await baseQuery
            .OrderBy(x => x.EmpresaNome)
            .ThenBy(x => x.Cedente.Nome)
            .ThenBy(x => x.Cedente.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new
            {
                x.Cedente.Id,
                x.Cedente.ClienteId,
                x.EmpresaNome,
                x.Cedente.Nome,
                Email = x.Cedente.Email == null ? null : x.Cedente.Email.Value,
                Telefone = x.Cedente.Telefone == null ? null : x.Cedente.Telefone.Value,
                x.Cedente.Endereco,
                x.Cedente.Status,
                x.Cedente.CreatedAt,
                DocumentoTipo = EF.Property<string>(x.Cedente, "DocumentoTipo"),
                CpfValue = EF.Property<string?>(x.Cedente, "CpfValue"),
                CnpjValue = EF.Property<string?>(x.Cedente, "CnpjCedenteValue"),
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var dtos = items.Select(x =>
        {
            var isPf = x.DocumentoTipo == "PF";
            var documento = isPf ? (x.CpfValue ?? string.Empty) : (x.CnpjValue ?? string.Empty);
            var cedenteTipo = isPf ? Onboarding.Domain.Aggregates.CedenteAggregate.CedenteTipo.PF
                                   : Onboarding.Domain.Aggregates.CedenteAggregate.CedenteTipo.PJ;

            return new AdminCedenteDto(
                x.Id,
                x.ClienteId,
                x.EmpresaNome,
                documento,
                x.Nome,
                x.Email,
                x.Telefone,
                x.Endereco,
                cedenteTipo,
                x.Status,
                x.CreatedAt);
        }).ToList();

        return new PaginatedResult<AdminCedenteDto>(dtos, totalCount, query.Page, query.PageSize);
    }
}
