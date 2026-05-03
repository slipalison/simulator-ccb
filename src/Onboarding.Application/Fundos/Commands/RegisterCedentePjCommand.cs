namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Command: register a new Cedente PJ with CNPJ (CAD-15).
/// Company-scoped per D-01 — CompanyId comes from ICurrentCompanyService.
/// </summary>
public sealed record RegisterCedentePjCommand(
    string Cnpj,
    string RazaoSocial,
    string? Email,
    string? Telefone,
    string? Endereco,
    string ActorSub,
    string ActorEmail);