using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Infrastructure.Repositories;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Repositories;

/// <summary>
/// InMemory unit tests for ConsultoriaFundoRepository.
/// Covers: AddAsync, SaveAsync, GetByIdAsync, ExistsByCnpjAsync, GetPagedByCompanyAsync (no search).
/// Limited fidelity:
///   - GetPagedByCompanyAsync with search uses EF.Functions.ILike → Integration-only.
///   - Client-side CNPJ filter (HashSet matching) is tested via Integration.Tests.
/// </summary>
public sealed class ConsultoriaFundoRepositoryTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    private static ConsultoriaFundo CreateConsultoria(
        Guid? clientId = null,
        string? cnpj = null,
        string? razaoSocial = null)
        => ConsultoriaFundo.Register(
            razaoSocial ?? "Consultoria Teste Ltda",
            cnpj ?? "11222333000181",
            clientId ?? CompanyId);

    [Fact]
    public async Task AddAsync_ValidConsultoria_PersistsEntity()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new ConsultoriaFundoRepository(db);
        var consultoria = CreateConsultoria(razaoSocial: "Gestora ABC");

        await repo.AddAsync(consultoria);

        var found = await repo.GetByIdAsync(consultoria.Id);
        found.ShouldNotBeNull();
        found!.RazaoSocial.ShouldBe("Gestora ABC");
    }

    [Fact]
    public async Task SaveAsync_UpdatesConsultoria()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new ConsultoriaFundoRepository(db);
        var consultoria = CreateConsultoria();
        await repo.AddAsync(consultoria);

        consultoria.Update("Updated Name", null, null, null, ConsultoriaFundoStatus.INATIVO);
        await repo.SaveAsync(consultoria);

        var updated = await repo.GetByIdAsync(consultoria.Id);
        updated.ShouldNotBeNull();
        updated!.RazaoSocial.ShouldBe("Updated Name");
        updated.Status.ShouldBe(ConsultoriaFundoStatus.INATIVO);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new ConsultoriaFundoRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ExistsByCnpjAsync_ExistingCnpjForCompany_ReturnsTrue()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new ConsultoriaFundoRepository(db);
        await repo.AddAsync(CreateConsultoria(cnpj: "11222333000181"));

        var exists = await repo.ExistsByCnpjAsync("11222333000181", CompanyId);

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsByCnpjAsync_DifferentCompany_ReturnsFalse()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new ConsultoriaFundoRepository(db);
        await repo.AddAsync(CreateConsultoria(cnpj: "11222333000181"));

        var exists = await repo.ExistsByCnpjAsync("11222333000181", Guid.NewGuid());

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsByCnpjAsync_NotFound_ReturnsFalse()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new ConsultoriaFundoRepository(db);

        var exists = await repo.ExistsByCnpjAsync("62232889000190", CompanyId);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetPagedByCompanyAsync_NoSearch_ReturnsForCompany()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new ConsultoriaFundoRepository(db);
        var otherCompanyId = Guid.NewGuid();

        await repo.AddAsync(CreateConsultoria(CompanyId, "11222333000181", "A"));
        await repo.AddAsync(CreateConsultoria(CompanyId, "11222333000181", "B"));
        await repo.AddAsync(CreateConsultoria(otherCompanyId, "11222333000181", "C")); // other company

        var (items, total) = await repo.GetPagedByCompanyAsync(CompanyId, 1, 10, null);

        total.ShouldBe(2);
        items.ShouldAllBe(c => c.ClienteId == CompanyId);
    }

    [Fact]
    public async Task GetPagedByCompanyAsync_Page2_ReturnsRemainingItems()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new ConsultoriaFundoRepository(db);

        for (var i = 0; i < 5; i++)
        {
            await repo.AddAsync(CreateConsultoria(CompanyId, "11222333000181", $"Cons {i}"));
        }

        var (items, total) = await repo.GetPagedByCompanyAsync(CompanyId, 2, 3, null);

        total.ShouldBe(5);
        items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetPagedByCompanyAsync_EmptyDatabase_ReturnsZero()
    {
        await using var db = InMemoryDbContextFactory.Create(CompanyId);
        var repo = new ConsultoriaFundoRepository(db);

        var (items, total) = await repo.GetPagedByCompanyAsync(CompanyId, 1, 10, null);

        total.ShouldBe(0);
        items.ShouldBeEmpty();
    }
}
