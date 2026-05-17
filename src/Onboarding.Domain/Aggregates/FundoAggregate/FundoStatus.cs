using System.Collections.Frozen;

namespace Onboarding.Domain.Aggregates.FundoAggregate;

/// <summary>
/// Represents the lifecycle status of a Fundo.
/// Valid transitions (D-02):
///   RASCUNHO → ATIVO
///   ATIVO ↔ SUSPENSO
///   ATIVO → EM_LIQUIDACAO
///   EM_LIQUIDACAO → ENCERRADO
/// All other transitions are invalid and must be rejected.
/// </summary>
public enum FundoStatus
{
    RASCUNHO = 1,
    ATIVO = 2,
    SUSPENSO = 3,
    EM_LIQUIDACAO = 4,
    ENCERRADO = 5
}

/// <summary>
/// Static helper for FundoStatus state machine validation (D-02, D-07).
/// Placed alongside the enum for discoverability. Not using State Pattern —
/// 5 states and ~5 transitions don't justify 5+ classes (YAGNI).
/// </summary>
public static class FundoStatusValidator
{
    private static readonly FrozenDictionary<(FundoStatus From, FundoStatus To), bool> _validTransitions =
        new Dictionary<(FundoStatus, FundoStatus), bool>
        {
            { (FundoStatus.RASCUNHO, FundoStatus.ATIVO), true },
            { (FundoStatus.ATIVO, FundoStatus.SUSPENSO), true },
            { (FundoStatus.SUSPENSO, FundoStatus.ATIVO), true },
            { (FundoStatus.ATIVO, FundoStatus.EM_LIQUIDACAO), true },
            { (FundoStatus.EM_LIQUIDACAO, FundoStatus.ENCERRADO), true },
        }.ToFrozenDictionary();

    /// <summary>
    /// Returns true if the transition from one status to another is valid per D-02.
    /// </summary>
    public static bool CanTransitionTo(FundoStatus from, FundoStatus to) =>
        _validTransitions.TryGetValue((from, to), out var valid) && valid;
}