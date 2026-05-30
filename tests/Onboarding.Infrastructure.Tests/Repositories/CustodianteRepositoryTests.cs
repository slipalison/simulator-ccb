using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Infrastructure.Repositories;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Repositories;

/// <summary>
/// InMemory unit tests for CustodianteRepository.
/// Covers: AddAsync, SaveAsync, GetByIdAsync, ExistsByCnpjAsync, GetPagedByCompanyAsync (no search).
/// Limited fidelity: EF.Functions.ILike search → Integration-only.
/// </summary>
public sealed class CustodianteRepositoryTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    private static Custodiante CreateCustodiante(
        Guid? clientId = null,
        string? cnpj = null,
        string? razaoSocial = null)
        => Custodiante.Register(
            razaoSocial ?? "Custodiante Teste Ltda",
            cnpj ?? "11222333000181",
            clientId ?? CompanyId);

    [Fact]
    public async Task AddAsync_ValidCustodiante_PersistsEntity()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new CustodianteRepository(db);
        var custodiante = CreateCustodiante(razaoSocial: "Banco Custódia S.A.");

        await repo.AddAsync(custodiante);

        var found = await repo.GetByIdAsync(custodiante.Id);
        found.ShouldNotBeNull();
        found!.RazaoSocial.ShouldBe("Banco Custódia S.A.");
    }

    [Fact]
    public async Task SaveAsync_UpdatesCustodiante()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new CustodianteRepository(db);
        var custodiante = CreateCustodiante();
        await repo.AddAsync(custodiante);

        custodiante.Update("Banco Atualizado", "INT-001", null, null, CustodianteStatus.INATIVO);
        await repo.SaveAsync(custodiante);

        var updated = await repo.GetByIdAsync(custodiante.Id);
        updated.ShouldNotBeNull();
        updated!.RazaoSocial.ShouldBe("Banco Atualizado");
        updated.Status.ShouldBe(CustodianteStatus.INATIVO);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new CustodianteRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ExistsByCnpjAsync_ExistingCnpjForCompany_ReturnsTrue()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new CustodianteRepository(db);
        await repo.AddAsync(CreateCustodiante(cnpj: "11222333000181"));

        var exists = await repo.ExistsByCnpjAsync("11222333000181", CompanyId);

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsByCnpjAsync_DifferentCompany_ReturnsFalse()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new CustodianteRepository(db);
        await repo.AddAsync(CreateCustodiante(cnpj: "11222333000181"));

        var exists = await repo.ExistsByCnpjAsync("11222333000181", Guid.NewGuid());

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsByCnpjAsync_NotFound_ReturnsFalse()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new CustodianteRepository(db);

        var exists = await repo.ExistsByCnpjAsync("62232889000190", CompanyId);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetPagedByCompanyAsync_NoSearch_ReturnsForCompany()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new CustodianteRepository(db);
        var otherCompanyId = Guid.NewGuid();

        await repo.AddAsync(CreateCustodiante(CompanyId, "11222333000181", "Cust A"));
        await repo.AddAsync(CreateCustodiante(CompanyId, "11222333000181", "Cust B"));
        await repo.AddAsync(CreateCustodiante(otherCompanyId, "11222333000181", "Cust C"));

        var (items, total) = await repo.GetPagedByCompanyAsync(CompanyId, 1, 10, null);

        total.ShouldBe(2);
        items.ShouldAllBe(c => c.ClienteId == CompanyId);
    }

    [Fact]
    public async Task GetPagedByCompanyAsync_Page2_ReturnsRemainingItems()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new CustodianteRepository(db);

        for (var i = 0; i < 5; i++)
        {
            await repo.AddAsync(CreateCustodiante(CompanyId, "11222333000181", $"Cust {i}"));
        }

        var (items, total) = await repo.GetPagedByCompanyAsync(CompanyId, 2, 3, null);

        total.ShouldBe(5);
        items.Count.ShouldBe(2);
    }
}
