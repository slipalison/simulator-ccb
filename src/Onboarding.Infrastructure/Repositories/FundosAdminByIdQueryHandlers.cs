using Microsoft.EntityFrameworkCore;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Queries.Admin;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// Cross-company admin GET-by-id query handlers for the Fundos module (D-8, D-12).
/// All handlers use IgnoreQueryFilters() — no tenant isolation applied.
/// Each query joins with Companies table to expose EmpresaNome + ClienteId.
/// SECURITY: These handlers MUST only be consumed by AdminFundosController
/// which requires Policy = CrossCompanyAccess (BearerBackoffice).
/// </summary>

/// <summary>
/// Returns a single AdminFundoDto for any company. Returns null if not found.
/// </summary>
public sealed class GetAdminFundoByIdQueryHandler
    : IQueryHandler<GetAdminFundoByIdQuery, AdminFundoDto?>
{
    private readonly AppDbContext _db;

    public GetAdminFundoByIdQueryHandler(AppDbContext db) => _db = db;

    public async Task<AdminFundoDto?> HandleAsync(
        GetAdminFundoByIdQuery query, CancellationToken ct = default)
    {
        // IgnoreQueryFilters — cross-company admin read (D-8, D-12).
        return await _db.Fundos
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(f => f.Id == query.Id)
            .Join(
                _db.Companies.AsNoTracking(),
                f => f.ClienteId,
                c => c.Id,
                (f, c) => new AdminFundoDto(
                    f.Id,
                    f.ClienteId,
                    c.RazaoSocial,
                    f.Nome,
                    f.Cnpj.Value,
                    f.ConsultoriaFundoId,
                    f.CustodianteId,
                    f.TipoFundo,
                    f.ClasseAnbima,
                    f.Segmento,
                    f.DataConstituicao,
                    f.Status,
                    f.CreatedAt))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Returns a single AdminConsultoriaFundoDto for any company. Returns null if not found.
/// </summary>
public sealed class GetAdminConsultoriaFundoByIdQueryHandler
    : IQueryHandler<GetAdminConsultoriaFundoByIdQuery, AdminConsultoriaFundoDto?>
{
    private readonly AppDbContext _db;

    public GetAdminConsultoriaFundoByIdQueryHandler(AppDbContext db) => _db = db;

    public async Task<AdminConsultoriaFundoDto?> HandleAsync(
        GetAdminConsultoriaFundoByIdQuery query, CancellationToken ct = default)
    {
        return await _db.ConsultoriasFundo
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Join(
                _db.Companies.AsNoTracking(),
                c => c.ClienteId,
                co => co.Id,
                (c, co) => new AdminConsultoriaFundoDto(
                    c.Id,
                    c.ClienteId,
                    co.RazaoSocial,
                    c.RazaoSocial,
                    c.NomeFantasia,
                    c.Cnpj.Value,
                    c.Email == null ? null : c.Email.Value,
                    c.Telefone == null ? null : c.Telefone.Value,
                    c.Status,
                    c.CreatedAt))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Returns a single AdminCustodianteDto for any company. Returns null if not found.
/// </summary>
public sealed class GetAdminCustodianteByIdQueryHandler
    : IQueryHandler<GetAdminCustodianteByIdQuery, AdminCustodianteDto?>
{
    private readonly AppDbContext _db;

    public GetAdminCustodianteByIdQueryHandler(AppDbContext db) => _db = db;

    public async Task<AdminCustodianteDto?> HandleAsync(
        GetAdminCustodianteByIdQuery query, CancellationToken ct = default)
    {
        return await _db.Custodiantes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(cu => cu.Id == query.Id)
            .Join(
                _db.Companies.AsNoTracking(),
                cu => cu.ClienteId,
                co => co.Id,
                (cu, co) => new AdminCustodianteDto(
                    cu.Id,
                    cu.ClienteId,
                    co.RazaoSocial,
                    cu.RazaoSocial,
                    cu.CodigoInterno,
                    cu.Cnpj.Value,
                    cu.Email == null ? null : cu.Email.Value,
                    cu.Telefone == null ? null : cu.Telefone.Value,
                    cu.Status,
                    cu.CreatedAt))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Returns a single AdminCedenteDto for any company. Returns null if not found.
/// Cedente document type (PF/PJ) is read from shadow property "DocumentoTipo" (D-09/CR-03).
/// </summary>
public sealed class GetAdminCedenteByIdQueryHandler
    : IQueryHandler<GetAdminCedenteByIdQuery, AdminCedenteDto?>
{
    private readonly AppDbContext _db;

    public GetAdminCedenteByIdQueryHandler(AppDbContext db) => _db = db;

    public async Task<AdminCedenteDto?> HandleAsync(
        GetAdminCedenteByIdQuery query, CancellationToken ct = default)
    {
        var row = await _db.Cedentes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(ce => ce.Id == query.Id)
            .Join(
                _db.Companies.AsNoTracking(),
                ce => ce.ClienteId,
                co => co.Id,
                (ce, co) => new
                {
                    ce.Id,
                    ce.ClienteId,
                    EmpresaNome = co.RazaoSocial,
                    ce.Nome,
                    Email = ce.Email == null ? null : ce.Email.Value,
                    Telefone = ce.Telefone == null ? null : ce.Telefone.Value,
                    ce.Endereco,
                    ce.Status,
                    ce.CreatedAt,
                    DocumentoTipo = EF.Property<string>(ce, "DocumentoTipo"),
                    CpfValue = EF.Property<string?>(ce, "CpfValue"),
                    CnpjValue = EF.Property<string?>(ce, "CnpjCedenteValue"),
                })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (row is null) return null;

        var isPf = row.DocumentoTipo == "PF";
        var documento = isPf ? (row.CpfValue ?? string.Empty) : (row.CnpjValue ?? string.Empty);
        var cedenteTipo = isPf
            ? Onboarding.Domain.Aggregates.CedenteAggregate.CedenteTipo.PF
            : Onboarding.Domain.Aggregates.CedenteAggregate.CedenteTipo.PJ;

        return new AdminCedenteDto(
            row.Id,
            row.ClienteId,
            row.EmpresaNome,
            documento,
            row.Nome,
            row.Email,
            row.Telefone,
            row.Endereco,
            cedenteTipo,
            row.Status,
            row.CreatedAt);
    }
}
