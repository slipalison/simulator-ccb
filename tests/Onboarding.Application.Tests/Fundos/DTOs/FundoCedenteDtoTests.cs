using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.DTOs;

/// <summary>
/// Tests for FundoCedenteDto — the legacy owned-entity DTO inside the Fundo aggregate.
/// Exercises property exposure and value-object round-trips.
/// </summary>
public sealed class FundoCedenteDtoTests
{
    // -------------------------------------------------------------------------
    // Constructor / property exposure
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_WithAllProperties_SetsAllMembers()
    {
        var cedenteId = Guid.NewGuid();
        var dataInicio = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);
        var dataFim = new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero);

        var dto = new FundoCedenteDto(
            CedenteId: cedenteId,
            LimiteExposicaoPercentual: 50.5m,
            LimiteExposicaoValor: 1_000_000m,
            DataInicio: dataInicio,
            DataFim: dataFim,
            Status: FundoCedenteStatus.ATIVO);

        dto.CedenteId.ShouldBe(cedenteId);
        dto.LimiteExposicaoPercentual.ShouldBe(50.5m);
        dto.LimiteExposicaoValor.ShouldBe(1_000_000m);
        dto.DataInicio.ShouldBe(dataInicio);
        dto.DataFim.ShouldBe(dataFim);
        dto.Status.ShouldBe(FundoCedenteStatus.ATIVO);
    }

    [Fact]
    public void Constructor_WithNullOptionals_SetsNullsCorrectly()
    {
        var cedenteId = Guid.NewGuid();
        var dataInicio = DateTimeOffset.UtcNow;

        var dto = new FundoCedenteDto(
            CedenteId: cedenteId,
            LimiteExposicaoPercentual: 25m,
            LimiteExposicaoValor: null,
            DataInicio: dataInicio,
            DataFim: null,
            Status: FundoCedenteStatus.INATIVO);

        dto.LimiteExposicaoValor.ShouldBeNull();
        dto.DataFim.ShouldBeNull();
        dto.Status.ShouldBe(FundoCedenteStatus.INATIVO);
    }

    // -------------------------------------------------------------------------
    // Record equality (structural)
    // -------------------------------------------------------------------------

    [Fact]
    public void Equality_WhenSameValues_ReturnsTrue()
    {
        var cedenteId = Guid.NewGuid();
        var dataInicio = DateTimeOffset.UtcNow;

        var dto1 = new FundoCedenteDto(cedenteId, 30m, null, dataInicio, null, FundoCedenteStatus.ATIVO);
        var dto2 = new FundoCedenteDto(cedenteId, 30m, null, dataInicio, null, FundoCedenteStatus.ATIVO);

        dto1.ShouldBe(dto2);
    }

    [Fact]
    public void Equality_WhenDifferentCedenteId_ReturnsFalse()
    {
        var dataInicio = DateTimeOffset.UtcNow;

        var dto1 = new FundoCedenteDto(Guid.NewGuid(), 30m, null, dataInicio, null, FundoCedenteStatus.ATIVO);
        var dto2 = new FundoCedenteDto(Guid.NewGuid(), 30m, null, dataInicio, null, FundoCedenteStatus.ATIVO);

        dto1.ShouldNotBe(dto2);
    }

    [Fact]
    public void Equality_WhenDifferentStatus_ReturnsFalse()
    {
        var cedenteId = Guid.NewGuid();
        var dataInicio = DateTimeOffset.UtcNow;

        var dto1 = new FundoCedenteDto(cedenteId, 30m, null, dataInicio, null, FundoCedenteStatus.ATIVO);
        var dto2 = new FundoCedenteDto(cedenteId, 30m, null, dataInicio, null, FundoCedenteStatus.INATIVO);

        dto1.ShouldNotBe(dto2);
    }

    // -------------------------------------------------------------------------
    // Deconstruct / with-expression (record features exercised)
    // -------------------------------------------------------------------------

    [Fact]
    public void WithExpression_OverridingStatus_CreatesNewDtoWithUpdatedStatus()
    {
        var cedenteId = Guid.NewGuid();
        var dataInicio = DateTimeOffset.UtcNow;
        var original = new FundoCedenteDto(cedenteId, 30m, null, dataInicio, null, FundoCedenteStatus.ATIVO);

        var updated = original with { Status = FundoCedenteStatus.INATIVO };

        updated.Status.ShouldBe(FundoCedenteStatus.INATIVO);
        updated.CedenteId.ShouldBe(cedenteId);
        updated.LimiteExposicaoPercentual.ShouldBe(30m);
    }

    [Fact]
    public void WithExpression_OverridingLimiteValor_PropagatesNewValue()
    {
        var dataInicio = DateTimeOffset.UtcNow;
        var original = new FundoCedenteDto(Guid.NewGuid(), 50m, null, dataInicio, null, FundoCedenteStatus.ATIVO);

        var updated = original with { LimiteExposicaoValor = 500_000m };

        updated.LimiteExposicaoValor.ShouldBe(500_000m);
        updated.LimiteExposicaoPercentual.ShouldBe(50m);
    }

    // -------------------------------------------------------------------------
    // FundoCedenteStatus enum coverage
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(FundoCedenteStatus.ATIVO)]
    [InlineData(FundoCedenteStatus.INATIVO)]
    public void Constructor_AllStatusValues_ArePropagatedCorrectly(FundoCedenteStatus status)
    {
        var dto = new FundoCedenteDto(Guid.NewGuid(), 10m, null, DateTimeOffset.UtcNow, null, status);
        dto.Status.ShouldBe(status);
    }

    // -------------------------------------------------------------------------
    // Sentinel value -1 = unlimited exposure (D-04)
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_WithSentinelMinusOne_IsStoredAsIs()
    {
        // D-04: -1 is a sentinel for "unlimited" exposure percentage
        var dto = new FundoCedenteDto(
            Guid.NewGuid(), -1m, null, DateTimeOffset.UtcNow, null, FundoCedenteStatus.ATIVO);

        dto.LimiteExposicaoPercentual.ShouldBe(-1m);
    }
}
