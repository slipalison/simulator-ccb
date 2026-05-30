using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Aggregates.FundoTipoAtivoAggregate;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Repositories;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Repositories;

/// <summary>
/// InMemory unit tests for FundoTipoAtivoAggregateRepository.
/// Limited fidelity: partial unique index NOT enforced in InMemory.
/// Integration.Tests is the primary coverage for uniqueness enforcement (REL-09).
/// </summary>
public sealed class FundoTipoAtivoAggregateRepositoryTests
{
    private static FundoTipoAtivoAggregate CreateAssociation(
        Guid? fundoId = null,
        Guid? tipoAtivoId = null)
        => FundoTipoAtivoAggregate.Create(
            fundoId ?? Guid.NewGuid(),
            tipoAtivoId ?? Guid.NewGuid(),
            LimiteExposicao.FromPercentual(25m),
            JanelaVigencia.Create(DateTimeOffset.UtcNow));

    [Fact]
    public async Task AddAsync_ValidAssociation_PersistsEntity()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new FundoTipoAtivoAggregateRepository(db);
        var assoc = CreateAssociation();

        await repo.AddAsync(assoc);

        var found = await repo.GetByIdAsync(assoc.Id);
        found.ShouldNotBeNull();
        found!.Status.ShouldBe(RelationshipStatus.ATIVO);
    }

    [Fact]
    public async Task SaveAsync_UpdatesStatus()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new FundoTipoAtivoAggregateRepository(db);
        var assoc = CreateAssociation();
        await repo.AddAsync(assoc);

        assoc.TransitionTo(RelationshipStatus.INATIVO);
        await repo.SaveAsync(assoc);

        var updated = await repo.GetByIdAsync(assoc.Id);
        updated!.Status.ShouldBe(RelationshipStatus.INATIVO);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new FundoTipoAtivoAggregateRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ExistsActiveAsync_ActiveAssociation_ReturnsTrue()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new FundoTipoAtivoAggregateRepository(db);
        var fundoId = Guid.NewGuid();
        var tipoAtivoId = Guid.NewGuid();
        await repo.AddAsync(CreateAssociation(fundoId, tipoAtivoId));

        var exists = await repo.ExistsActiveAsync(fundoId, tipoAtivoId);

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsActiveAsync_NoAssociation_ReturnsFalse()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new FundoTipoAtivoAggregateRepository(db);

        var exists = await repo.ExistsActiveAsync(Guid.NewGuid(), Guid.NewGuid());

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetPagedByFundoAsync_ReturnsOnlyForFundo()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new FundoTipoAtivoAggregateRepository(db);
        var fundoId = Guid.NewGuid();

        await repo.AddAsync(CreateAssociation(fundoId));
        await repo.AddAsync(CreateAssociation(fundoId));
        await repo.AddAsync(CreateAssociation(Guid.NewGuid())); // different fundo

        var (items, total) = await repo.GetPagedByFundoAsync(fundoId, 1, 10);

        total.ShouldBe(2);
        items.ShouldAllBe(a => a.FundoId == fundoId);
    }

    [Fact]
    public async Task GetPagedByFundoAsync_Page2_ReturnsRemainingItems()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new FundoTipoAtivoAggregateRepository(db);
        var fundoId = Guid.NewGuid();

        for (var i = 0; i < 5; i++)
            await repo.AddAsync(CreateAssociation(fundoId));

        var (items, total) = await repo.GetPagedByFundoAsync(fundoId, 2, 3);

        total.ShouldBe(5);
        items.Count.ShouldBe(2);
    }
}
