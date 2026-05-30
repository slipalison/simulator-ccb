using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Repositories;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Repositories;

/// <summary>
/// InMemory unit tests for FundoCedenteAggregateRepository.
/// Covers: AddAsync, SaveAsync, GetByIdAsync, ExistsActiveAsync, GetPagedByFundoAsync.
/// Limited fidelity:
///   - Partial unique index "WHERE status = 'ATIVO'" is NOT enforced by InMemory (REL-09).
///     Integration.Tests (Testcontainers) is the authoritative test for uniqueness enforcement.
///   - FK constraints (Fundo, Cedente) are not enforced by InMemory.
/// </summary>
public sealed class FundoCedenteAggregateRepositoryTests
{
    private static FundoCedenteAggregate CreateAssociation(
        Guid? fundoId = null,
        Guid? cedenteId = null)
        => FundoCedenteAggregate.Create(
            fundoId ?? Guid.NewGuid(),
            cedenteId ?? Guid.NewGuid(),
            LimiteExposicao.FromPercentual(50m),
            JanelaVigencia.Create(DateTimeOffset.UtcNow));

    [Fact]
    public async Task AddAsync_ValidAssociation_PersistsEntity()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new FundoCedenteAggregateRepository(db);
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
        var repo = new FundoCedenteAggregateRepository(db);
        var assoc = CreateAssociation();
        await repo.AddAsync(assoc);

        assoc.TransitionTo(RelationshipStatus.INATIVO);
        await repo.SaveAsync(assoc);

        var updated = await repo.GetByIdAsync(assoc.Id);
        updated.ShouldNotBeNull();
        updated!.Status.ShouldBe(RelationshipStatus.INATIVO);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new FundoCedenteAggregateRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ExistsActiveAsync_ActiveAssociation_ReturnsTrue()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new FundoCedenteAggregateRepository(db);
        var fundoId = Guid.NewGuid();
        var cedenteId = Guid.NewGuid();
        var assoc = CreateAssociation(fundoId, cedenteId);
        await repo.AddAsync(assoc);

        var exists = await repo.ExistsActiveAsync(fundoId, cedenteId);

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsActiveAsync_InactiveAssociation_ReturnsFalse()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new FundoCedenteAggregateRepository(db);
        var fundoId = Guid.NewGuid();
        var cedenteId = Guid.NewGuid();
        var assoc = CreateAssociation(fundoId, cedenteId);
        await repo.AddAsync(assoc);

        assoc.TransitionTo(RelationshipStatus.INATIVO);
        await repo.SaveAsync(assoc);

        var exists = await repo.ExistsActiveAsync(fundoId, cedenteId);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsActiveAsync_NoPair_ReturnsFalse()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new FundoCedenteAggregateRepository(db);

        var exists = await repo.ExistsActiveAsync(Guid.NewGuid(), Guid.NewGuid());

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetPagedByFundoAsync_ReturnsAssociationsForFundo()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new FundoCedenteAggregateRepository(db);
        var fundoId = Guid.NewGuid();
        var otherFundoId = Guid.NewGuid();

        await repo.AddAsync(CreateAssociation(fundoId));
        await repo.AddAsync(CreateAssociation(fundoId));
        await repo.AddAsync(CreateAssociation(otherFundoId)); // different fundo

        var (items, total) = await repo.GetPagedByFundoAsync(fundoId, 1, 10);

        total.ShouldBe(2);
        items.ShouldAllBe(a => a.FundoId == fundoId);
    }

    [Fact]
    public async Task GetPagedByFundoAsync_Page2_ReturnsRemainingItems()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new FundoCedenteAggregateRepository(db);
        var fundoId = Guid.NewGuid();

        for (var i = 0; i < 5; i++)
            await repo.AddAsync(CreateAssociation(fundoId));

        var (items, total) = await repo.GetPagedByFundoAsync(fundoId, 2, 3);

        total.ShouldBe(5);
        items.Count.ShouldBe(2);
    }
}
