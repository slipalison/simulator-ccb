using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Repositories;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Repositories;

/// <summary>
/// InMemory unit tests for FundoRepository.
/// Covers: AddAsync, SaveAsync, GetByIdAsync, ExistsByCnpjAsync, GetPagedByCompanyAsync (no search).
/// Limited fidelity:
///   - EF.Functions.ILike in GetPagedByCompanyAsync search path throws in InMemory.
///     Search-path tests are Integration-only.
///   - HasQueryFilter is active for the configured CompanyId. IgnoreQueryFilters() bypasses it.
///   - FK constraints to Company/ConsultoriaFundo/Custodiante are NOT enforced by InMemory.
/// </summary>
public sealed class FundoRepositoryTests
{
    private static Fundo CreateFundo(
        Guid? clientId = null,
        string? cnpj = null,
        string? nome = null)
        => Fundo.Register(
            nome ?? "Fundo Teste FIC FIA",
            cnpj ?? "11222333000181",
            clientId ?? Guid.NewGuid(),
            Guid.NewGuid(), // consultoriaFundoId
            Guid.NewGuid(), // custodianteId
            TipoFundo.RendaVariavel);

    [Fact]
    public async Task AddAsync_ValidFundo_PersistsEntity()
    {
        var companyId = Guid.NewGuid();
        await using var db = InMemoryDbContextFactory.Create(companyId);
        var repo = new FundoRepository(db);
        var fundo = CreateFundo(companyId, "11222333000181", "Fundo ABC");

        await repo.AddAsync(fundo);

        // GetByIdAsync uses IgnoreQueryFilters
        var found = await repo.GetByIdAsync(fundo.Id);
        found.ShouldNotBeNull();
        found!.Nome.ShouldBe("Fundo ABC");
    }

    [Fact]
    public async Task SaveAsync_TransitionsStatus()
    {
        var companyId = Guid.NewGuid();
        await using var db = InMemoryDbContextFactory.Create(companyId);
        var repo = new FundoRepository(db);
        var fundo = CreateFundo(companyId);
        await repo.AddAsync(fundo);

        fundo.TransitionTo(FundoStatus.ATIVO);
        await repo.SaveAsync(fundo);

        var updated = await repo.GetByIdAsync(fundo.Id);
        updated.ShouldNotBeNull();
        updated!.Status.ShouldBe(FundoStatus.ATIVO);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new FundoRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ExistsByCnpjAsync_ExistingCnpjForCompany_ReturnsTrue()
    {
        var companyId = Guid.NewGuid();
        await using var db = InMemoryDbContextFactory.Create(companyId);
        var repo = new FundoRepository(db);
        var fundo = CreateFundo(companyId, "11222333000181");
        await repo.AddAsync(fundo);

        var exists = await repo.ExistsByCnpjAsync("11222333000181", companyId);

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsByCnpjAsync_CnpjForDifferentCompany_ReturnsFalse()
    {
        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        await using var db = InMemoryDbContextFactory.Create(companyId);
        var repo = new FundoRepository(db);
        var fundo = CreateFundo(companyId, "11222333000181");
        await repo.AddAsync(fundo);

        var exists = await repo.ExistsByCnpjAsync("11222333000181", otherCompanyId);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsByCnpjAsync_NoCnpj_ReturnsFalse()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new FundoRepository(db);

        var exists = await repo.ExistsByCnpjAsync("62232889000190", Guid.NewGuid());

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetPagedByCompanyAsync_NoSearch_ReturnsAllForCompany()
    {
        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        await using var db = InMemoryDbContextFactory.Create(companyId);
        var repo = new FundoRepository(db);

        await repo.AddAsync(CreateFundo(companyId, "11222333000181", "Fundo A"));
        await repo.AddAsync(CreateFundo(companyId, "11222333000181", "Fundo B"));
        await repo.AddAsync(CreateFundo(otherCompanyId, "11222333000181", "Fundo C")); // other company

        var (items, total) = await repo.GetPagedByCompanyAsync(companyId, 1, 10, null);

        total.ShouldBe(2);
        items.ShouldAllBe(f => f.ClienteId == companyId);
    }

    [Fact]
    public async Task GetPagedByCompanyAsync_EmptyDatabase_ReturnsZeroCount()
    {
        var companyId = Guid.NewGuid();
        await using var db = InMemoryDbContextFactory.Create(companyId);
        var repo = new FundoRepository(db);

        var (items, total) = await repo.GetPagedByCompanyAsync(companyId, 1, 10, null);

        total.ShouldBe(0);
        items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPagedByCompanyAsync_Page2_ReturnsRemainingItems()
    {
        var companyId = Guid.NewGuid();
        await using var db = InMemoryDbContextFactory.Create(companyId);
        var repo = new FundoRepository(db);

        for (var i = 0; i < 5; i++)
        {
            await repo.AddAsync(CreateFundo(companyId, "11222333000181", $"Fundo {i}"));
        }

        var (items, total) = await repo.GetPagedByCompanyAsync(companyId, 2, 3, null);

        total.ShouldBe(5);
        items.Count.ShouldBe(2);
    }
}
