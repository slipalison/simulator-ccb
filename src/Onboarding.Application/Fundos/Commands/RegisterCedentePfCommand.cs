namespace Onboarding.Application.Fundos.Commands;

/// <summary>
/// Command: register a new Cedente PF with CPF (CAD-14).
/// Company-scoped per D-01 — CompanyId comes from ICurrentCompanyService.
/// </summary>
public sealed record RegisterCedentePfCommand(
    string Cpf,
    string Nome,
    string? Email,
    string? Telefone,
    string? Endereco,
    string ActorSub,
    string ActorEmail);