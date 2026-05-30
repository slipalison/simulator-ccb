using Onboarding.Application.Fundos.Queries.Admin;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Admin;

/// <summary>
/// Tests for AdminRelFundoTipoAtivoDto and AdminRelCedenteTipoAtivoDto
/// — cross-company admin relationship DTOs (D-8).
/// </summary>
public sealed class AdminRelDtoTests
{
    // =========================================================================
    // AdminRelFundoTipoAtivoDto
    // =========================================================================

    [Fact]
    public void AdminRelFundoTipoAtivoDto_Constructor_WithAllProperties_SetsAllMembers()
    {
        var id = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var fundoId = Guid.NewGuid();
        var tipoAtivoId = Guid.NewGuid();
        var dataInicio = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var dataFim = new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var createdAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var dto = new AdminRelFundoTipoAtivoDto(
            Id: id,
            ClienteId: clienteId,
            EmpresaNome: "Corp SA",
            FundoId: fundoId,
            FundoNome: "Fundo Alpha",
            TipoAtivoId: tipoAtivoId,
            LimitePercentual: 40m,
            LimiteValor: 2_000_000m,
            DataInicio: dataInicio,
            DataFim: dataFim,
            Status: RelationshipStatus.ATIVO,
            CreatedAt: createdAt);

        dto.Id.ShouldBe(id);
        dto.ClienteId.ShouldBe(clienteId);
        dto.EmpresaNome.ShouldBe("Corp SA");
        dto.FundoId.ShouldBe(fundoId);
        dto.FundoNome.ShouldBe("Fundo Alpha");
        dto.TipoAtivoId.ShouldBe(tipoAtivoId);
        dto.LimitePercentual.ShouldBe(40m);
        dto.LimiteValor.ShouldBe(2_000_000m);
        dto.DataInicio.ShouldBe(dataInicio);
        dto.DataFim.ShouldBe(dataFim);
        dto.Status.ShouldBe(RelationshipStatus.ATIVO);
        dto.CreatedAt.ShouldBe(createdAt);
    }

    [Fact]
    public void AdminRelFundoTipoAtivoDto_Constructor_WithNullOptionals_SetsNullsCorrectly()
    {
        var dataInicio = DateTimeOffset.UtcNow;
        var createdAt = DateTimeOffset.UtcNow;

        var dto = new AdminRelFundoTipoAtivoDto(
            Id: Guid.NewGuid(),
            ClienteId: Guid.NewGuid(),
            EmpresaNome: "Empresa Beta",
            FundoId: Guid.NewGuid(),
            FundoNome: "Fundo Beta",
            TipoAtivoId: Guid.NewGuid(),
            LimitePercentual: null,
            LimiteValor: null,
            DataInicio: dataInicio,
            DataFim: null,
            Status: RelationshipStatus.INATIVO,
            CreatedAt: createdAt);

        dto.LimitePercentual.ShouldBeNull();
        dto.LimiteValor.ShouldBeNull();
        dto.DataFim.ShouldBeNull();
        dto.Status.ShouldBe(RelationshipStatus.INATIVO);
    }

    [Fact]
    public void AdminRelFundoTipoAtivoDto_Constructor_WithHistoricoStatus_SetsCorrectly()
    {
        var dto = new AdminRelFundoTipoAtivoDto(
            Guid.NewGuid(), Guid.NewGuid(), "Empresa", Guid.NewGuid(), "Fundo",
            Guid.NewGuid(), 50m, null, DateTimeOffset.UtcNow, null,
            RelationshipStatus.HISTORICO, DateTimeOffset.UtcNow);

        dto.Status.ShouldBe(RelationshipStatus.HISTORICO);
    }

    [Fact]
    public void AdminRelFundoTipoAtivoDto_Equality_WhenSameValues_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var fundoId = Guid.NewGuid();
        var tipoAtivoId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var dto1 = new AdminRelFundoTipoAtivoDto(id, clienteId, "Corp", fundoId, "Fundo", tipoAtivoId, 10m, null, now, null, RelationshipStatus.ATIVO, now);
        var dto2 = new AdminRelFundoTipoAtivoDto(id, clienteId, "Corp", fundoId, "Fundo", tipoAtivoId, 10m, null, now, null, RelationshipStatus.ATIVO, now);

        dto1.ShouldBe(dto2);
    }

    [Fact]
    public void AdminRelFundoTipoAtivoDto_Equality_WhenDifferentStatus_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var fundoId = Guid.NewGuid();
        var tipoAtivoId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var dto1 = new AdminRelFundoTipoAtivoDto(id, clienteId, "Corp", fundoId, "Fundo", tipoAtivoId, 10m, null, now, null, RelationshipStatus.ATIVO, now);
        var dto2 = new AdminRelFundoTipoAtivoDto(id, clienteId, "Corp", fundoId, "Fundo", tipoAtivoId, 10m, null, now, null, RelationshipStatus.HISTORICO, now);

        dto1.ShouldNotBe(dto2);
    }

    [Fact]
    public void AdminRelFundoTipoAtivoDto_Equality_WhenDifferentClienteId_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var fundoId = Guid.NewGuid();
        var tipoAtivoId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var dto1 = new AdminRelFundoTipoAtivoDto(id, Guid.NewGuid(), "Corp", fundoId, "Fundo", tipoAtivoId, 10m, null, now, null, RelationshipStatus.ATIVO, now);
        var dto2 = new AdminRelFundoTipoAtivoDto(id, Guid.NewGuid(), "Corp", fundoId, "Fundo", tipoAtivoId, 10m, null, now, null, RelationshipStatus.ATIVO, now);

        dto1.ShouldNotBe(dto2);
    }

    [Fact]
    public void AdminRelFundoTipoAtivoDto_WithExpression_OverridingStatus_UpdatesStatus()
    {
        var now = DateTimeOffset.UtcNow;
        var original = new AdminRelFundoTipoAtivoDto(
            Guid.NewGuid(), Guid.NewGuid(), "Corp", Guid.NewGuid(), "Fundo",
            Guid.NewGuid(), 30m, null, now, null, RelationshipStatus.ATIVO, now);

        var updated = original with { Status = RelationshipStatus.INATIVO };

        updated.Status.ShouldBe(RelationshipStatus.INATIVO);
        updated.EmpresaNome.ShouldBe("Corp");
    }

    [Theory]
    [InlineData(RelationshipStatus.ATIVO)]
    [InlineData(RelationshipStatus.INATIVO)]
    [InlineData(RelationshipStatus.HISTORICO)]
    public void AdminRelFundoTipoAtivoDto_AllStatusValues_StoredCorrectly(RelationshipStatus status)
    {
        var now = DateTimeOffset.UtcNow;
        var dto = new AdminRelFundoTipoAtivoDto(
            Guid.NewGuid(), Guid.NewGuid(), "Corp", Guid.NewGuid(), "Fundo",
            Guid.NewGuid(), null, null, now, null, status, now);

        dto.Status.ShouldBe(status);
    }

    // =========================================================================
    // AdminRelCedenteTipoAtivoDto
    // =========================================================================

    [Fact]
    public void AdminRelCedenteTipoAtivoDto_Constructor_WithAllProperties_SetsAllMembers()
    {
        var id = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var cedenteId = Guid.NewGuid();
        var tipoAtivoId = Guid.NewGuid();
        var dataInicio = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var dataFim = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var createdAt = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);

        var dto = new AdminRelCedenteTipoAtivoDto(
            Id: id,
            ClienteId: clienteId,
            EmpresaNome: "Cedente Corp",
            CedenteId: cedenteId,
            TipoAtivoId: tipoAtivoId,
            LimitePercentual: 25m,
            LimiteValor: 500_000m,
            DataInicio: dataInicio,
            DataFim: dataFim,
            Status: RelationshipStatus.ATIVO,
            CreatedAt: createdAt);

        dto.Id.ShouldBe(id);
        dto.ClienteId.ShouldBe(clienteId);
        dto.EmpresaNome.ShouldBe("Cedente Corp");
        dto.CedenteId.ShouldBe(cedenteId);
        dto.TipoAtivoId.ShouldBe(tipoAtivoId);
        dto.LimitePercentual.ShouldBe(25m);
        dto.LimiteValor.ShouldBe(500_000m);
        dto.DataInicio.ShouldBe(dataInicio);
        dto.DataFim.ShouldBe(dataFim);
        dto.Status.ShouldBe(RelationshipStatus.ATIVO);
        dto.CreatedAt.ShouldBe(createdAt);
    }

    [Fact]
    public void AdminRelCedenteTipoAtivoDto_Constructor_WithNullOptionals_SetsNullsCorrectly()
    {
        var dataInicio = DateTimeOffset.UtcNow;
        var createdAt = DateTimeOffset.UtcNow;

        var dto = new AdminRelCedenteTipoAtivoDto(
            Id: Guid.NewGuid(),
            ClienteId: Guid.NewGuid(),
            EmpresaNome: "Empresa Gamma",
            CedenteId: Guid.NewGuid(),
            TipoAtivoId: Guid.NewGuid(),
            LimitePercentual: null,
            LimiteValor: null,
            DataInicio: dataInicio,
            DataFim: null,
            Status: RelationshipStatus.HISTORICO,
            CreatedAt: createdAt);

        dto.LimitePercentual.ShouldBeNull();
        dto.LimiteValor.ShouldBeNull();
        dto.DataFim.ShouldBeNull();
        dto.Status.ShouldBe(RelationshipStatus.HISTORICO);
    }

    [Fact]
    public void AdminRelCedenteTipoAtivoDto_Equality_WhenSameValues_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var cedenteId = Guid.NewGuid();
        var tipoAtivoId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var dto1 = new AdminRelCedenteTipoAtivoDto(id, clienteId, "Corp", cedenteId, tipoAtivoId, 10m, null, now, null, RelationshipStatus.ATIVO, now);
        var dto2 = new AdminRelCedenteTipoAtivoDto(id, clienteId, "Corp", cedenteId, tipoAtivoId, 10m, null, now, null, RelationshipStatus.ATIVO, now);

        dto1.ShouldBe(dto2);
    }

    [Fact]
    public void AdminRelCedenteTipoAtivoDto_Equality_WhenDifferentCedenteId_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var tipoAtivoId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var dto1 = new AdminRelCedenteTipoAtivoDto(id, clienteId, "Corp", Guid.NewGuid(), tipoAtivoId, 10m, null, now, null, RelationshipStatus.ATIVO, now);
        var dto2 = new AdminRelCedenteTipoAtivoDto(id, clienteId, "Corp", Guid.NewGuid(), tipoAtivoId, 10m, null, now, null, RelationshipStatus.ATIVO, now);

        dto1.ShouldNotBe(dto2);
    }

    [Fact]
    public void AdminRelCedenteTipoAtivoDto_Equality_WhenDifferentLimitePercentual_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var cedenteId = Guid.NewGuid();
        var tipoAtivoId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var dto1 = new AdminRelCedenteTipoAtivoDto(id, clienteId, "Corp", cedenteId, tipoAtivoId, 10m, null, now, null, RelationshipStatus.ATIVO, now);
        var dto2 = new AdminRelCedenteTipoAtivoDto(id, clienteId, "Corp", cedenteId, tipoAtivoId, 20m, null, now, null, RelationshipStatus.ATIVO, now);

        dto1.ShouldNotBe(dto2);
    }

    [Fact]
    public void AdminRelCedenteTipoAtivoDto_WithExpression_OverridingStatus_UpdatesStatus()
    {
        var now = DateTimeOffset.UtcNow;
        var original = new AdminRelCedenteTipoAtivoDto(
            Guid.NewGuid(), Guid.NewGuid(), "Corp", Guid.NewGuid(), Guid.NewGuid(),
            15m, null, now, null, RelationshipStatus.ATIVO, now);

        var updated = original with { Status = RelationshipStatus.HISTORICO };

        updated.Status.ShouldBe(RelationshipStatus.HISTORICO);
        updated.EmpresaNome.ShouldBe("Corp");
    }

    [Theory]
    [InlineData(RelationshipStatus.ATIVO)]
    [InlineData(RelationshipStatus.INATIVO)]
    [InlineData(RelationshipStatus.HISTORICO)]
    public void AdminRelCedenteTipoAtivoDto_AllStatusValues_StoredCorrectly(RelationshipStatus status)
    {
        var now = DateTimeOffset.UtcNow;
        var dto = new AdminRelCedenteTipoAtivoDto(
            Guid.NewGuid(), Guid.NewGuid(), "Corp", Guid.NewGuid(), Guid.NewGuid(),
            null, null, now, null, status, now);

        dto.Status.ShouldBe(status);
    }

    [Fact]
    public void AdminRelCedenteTipoAtivoDto_WithExpression_OverridingLimiteValor_UpdatesValue()
    {
        var now = DateTimeOffset.UtcNow;
        var original = new AdminRelCedenteTipoAtivoDto(
            Guid.NewGuid(), Guid.NewGuid(), "Corp", Guid.NewGuid(), Guid.NewGuid(),
            null, null, now, null, RelationshipStatus.ATIVO, now);

        var updated = original with { LimiteValor = 750_000m };

        updated.LimiteValor.ShouldBe(750_000m);
        updated.LimitePercentual.ShouldBeNull();
    }
}
