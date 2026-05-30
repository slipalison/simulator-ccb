using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Infrastructure.Repositories;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Repositories;

/// <summary>
/// InMemory unit tests for TipoAtivoRepository.
/// TipoAtivo is a global entity (no HasQueryFilter, no ClientId).
/// Limited fidelity: EF.Functions.ILike is NOT supported by InMemory — search tests require Integration.Tests.
/// Covers: AddAsync, SaveAsync, GetByIdAsync, ExistsByCodigoAsync, GetPagedAsync (no search).
/// </summary>
public sealed class TipoAtivoRepositoryTests
{
    private static TipoAtivo CreateTipoAtivo(string? codigo = null)
        => TipoAtivo.Register(
            codigo ?? $"RF-{Guid.NewGuid():N}".Substring(0, 8),
            "Titulo Publico Federal",
            TipoAtivoCategoria.RendaFixa);

    [Fact]
    public async Task AddAsync_ValidTipoAtivo_PersistsEntity()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new TipoAtivoRepository(db);
        var ta = CreateTipoAtivo("RF-001");

        await repo.AddAsync(ta);

        var found = await repo.GetByIdAsync(ta.Id);
        found.ShouldNotBeNull();
        found!.Codigo.ShouldBe("RF-001");
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingTipoAtivo()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new TipoAtivoRepository(db);
        var ta = CreateTipoAtivo("RF-002");
        await repo.AddAsync(ta);

        ta.Update("Updated Desc", "Subcategoria A", TipoAtivoStatus.INATIVO, 5);
        await repo.SaveAsync(ta);

        var updated = await repo.GetByIdAsync(ta.Id);
        updated.ShouldNotBeNull();
        updated!.Descricao.ShouldBe("Updated Desc");
        updated.Status.ShouldBe(TipoAtivoStatus.INATIVO);
        updated.OrdemExibicao.ShouldBe(5);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new TipoAtivoRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ExistsByCodigoAsync_ExistingCode_ReturnsTrue()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new TipoAtivoRepository(db);
        var ta = CreateTipoAtivo("RF-CODE");
        await repo.AddAsync(ta);

        var exists = await repo.ExistsByCodigoAsync("RF-CODE");

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsByCodigoAsync_MissingCode_ReturnsFalse()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new TipoAtivoRepository(db);

        var exists = await repo.ExistsByCodigoAsync("DOES-NOT-EXIST");

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetPagedAsync_NoItems_ReturnsEmptyResult()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new TipoAtivoRepository(db);

        var (items, totalCount) = await repo.GetPagedAsync(1, 10, null);

        items.ShouldBeEmpty();
        totalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetPagedAsync_MultipleItems_ReturnsPaginatedResult()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new TipoAtivoRepository(db);

        for (var i = 0; i < 5; i++)
        {
            var ta = TipoAtivo.Register($"RF-{i:D3}", $"Desc {i}", TipoAtivoCategoria.RendaFixa, ordemExibicao: i);
            await repo.AddAsync(ta);
        }

        var (items, totalCount) = await repo.GetPagedAsync(1, 3, null);

        totalCount.ShouldBe(5);
        items.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetPagedAsync_Page2_ReturnsRemainingItems()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new TipoAtivoRepository(db);

        for (var i = 0; i < 5; i++)
        {
            var ta = TipoAtivo.Register($"RV-{i:D3}", $"Desc {i}", TipoAtivoCategoria.RendaVariavel, ordemExibicao: i);
            await repo.AddAsync(ta);
        }

        var (items, totalCount) = await repo.GetPagedAsync(2, 3, null);

        totalCount.ShouldBe(5);
        items.Count.ShouldBe(2);
    }
}
