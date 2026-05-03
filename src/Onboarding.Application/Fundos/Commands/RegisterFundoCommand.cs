using Onboarding.Domain.Aggregates.FundoAggregate;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Command: register a new Fundo with RASCUNHO status (CAD-09).
/// Company-scoped per D-01 — CompanyId comes from ICurrentCompanyService.
/// </summary>
public sealed record RegisterFundoCommand(
    string Nome,
    string Cnpj,
    Guid ConsultoriaFundoId,
    Guid CustodianteId,
    TipoFundo TipoFundo,
    string? ClasseAnbima,
    string? Segmento,
    DateTimeOffset? DataConstituicao,
    string ActorSub,
    string ActorEmail);