namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Command: register a new Custodiante (CAD-05).
/// Company-scoped per D-01 — CompanyId comes from ICurrentCompanyService.
/// </summary>
public sealed record RegisterCustodianteCommand(
    string RazaoSocial,
    string Cnpj,
    string? CodigoInterno,
    string? Email,
    string? Telefone);