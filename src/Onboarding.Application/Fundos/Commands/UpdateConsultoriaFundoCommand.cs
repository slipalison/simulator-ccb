using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;

namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Command: update ConsultoriaFundo fields (CAD-03).
/// </summary>
public sealed record UpdateConsultoriaFundoCommand(
    Guid Id,
    string RazaoSocial,
    string? NomeFantasia,
    string? Email,
    string? Telefone,
    ConsultoriaFundoStatus Status,
    string ActorSub,
    string ActorEmail);