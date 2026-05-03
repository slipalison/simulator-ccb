namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Command: register a new ConsultoriaFundo (CAD-01).
/// Company-scoped per D-01 — CompanyId comes from ICurrentCompanyService.
/// </summary>
public sealed record RegisterConsultoriaFundoCommand(
    string RazaoSocial,
    string Cnpj,
    string? NomeFantasia,
    string? Email,
    string? Telefone);