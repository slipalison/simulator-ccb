using NSubstitute;
using Onboarding.Application.Admin.Queries;
using Onboarding.Application.Common;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Commands;

[Trait("Category", "Unit")]
public class GetPaginatedAdministratorsQueryHandlerTests
{
    private readonly IKeycloakUserService _keycloakMock = Substitute.For<IKeycloakUserService>();
    private readonly GetPaginatedAdministratorsQueryHandler _sut;

    public GetPaginatedAdministratorsQueryHandlerTests()
        => _sut = new GetPaginatedAdministratorsQueryHandler(_keycloakMock);

    private static IReadOnlyList<AdminUserDto> CreateAdmins() => new List<AdminUserDto>
    {
        new("id1", "alice@test.com", "Alice Admin", true, false),
        new("id2", "bob@test.com", "Bob Builder", false, false),
        new("id3", "charlie@test.com", "Charlie Chaplin", true, false),
    }.AsReadOnly();

    [Fact]
    public async Task HandleAsync_ReturnsAllAdmins_WhenNoFilters()
    {
        _keycloakMock.GetUsersByRoleAsync("backoffice", "admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateAdmins()));

        var result = await _sut.HandleAsync(new GetPaginatedAdministratorsQuery(1, 20, null, null, null));

        result.Items.Count.ShouldBe(3);
        result.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task HandleAsync_FiltersByName()
    {
        _keycloakMock.GetUsersByRoleAsync("backoffice", "admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateAdmins()));

        var result = await _sut.HandleAsync(new GetPaginatedAdministratorsQuery(1, 20, "Alice", null, null));

        result.Items.Count.ShouldBe(1);
        result.Items[0].FullName.ShouldBe("Alice Admin");
    }

    [Fact]
    public async Task HandleAsync_FiltersByName_CaseInsensitive()
    {
        _keycloakMock.GetUsersByRoleAsync("backoffice", "admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateAdmins()));

        var result = await _sut.HandleAsync(new GetPaginatedAdministratorsQuery(1, 20, "ALICE", null, null));

        result.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_FiltersByEmail()
    {
        _keycloakMock.GetUsersByRoleAsync("backoffice", "admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateAdmins()));

        var result = await _sut.HandleAsync(new GetPaginatedAdministratorsQuery(1, 20, null, "bob", null));

        result.Items.Count.ShouldBe(1);
        result.Items[0].Email.ShouldBe("bob@test.com");
    }

    [Fact]
    public async Task HandleAsync_FiltersByActiveStatus()
    {
        _keycloakMock.GetUsersByRoleAsync("backoffice", "admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateAdmins()));

        var result = await _sut.HandleAsync(new GetPaginatedAdministratorsQuery(1, 20, null, null, "active"));

        result.Items.Count.ShouldBe(2);
        result.Items.ShouldAllBe(i => i.IsEnabled);
    }

    [Fact]
    public async Task HandleAsync_FiltersByInactiveStatus()
    {
        _keycloakMock.GetUsersByRoleAsync("backoffice", "admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateAdmins()));

        var result = await _sut.HandleAsync(new GetPaginatedAdministratorsQuery(1, 20, null, null, "inactive"));

        result.Items.Count.ShouldBe(1);
        result.Items[0].IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_AppliesPagination()
    {
        _keycloakMock.GetUsersByRoleAsync("backoffice", "admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateAdmins()));

        var result = await _sut.HandleAsync(new GetPaginatedAdministratorsQuery(1, 2, null, null, null));

        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(3);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_Page2_ReturnsRemainingItems()
    {
        _keycloakMock.GetUsersByRoleAsync("backoffice", "admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateAdmins()));

        var result = await _sut.HandleAsync(new GetPaginatedAdministratorsQuery(2, 2, null, null, null));

        result.Items.Count.ShouldBe(1);
        result.TotalCount.ShouldBe(3);
        result.Page.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_CombinesNameAndStatusFilters()
    {
        _keycloakMock.GetUsersByRoleAsync("backoffice", "admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateAdmins()));

        var result = await _sut.HandleAsync(new GetPaginatedAdministratorsQuery(1, 20, "Alice", null, "active"));

        result.Items.Count.ShouldBe(1);
        result.Items[0].FullName.ShouldBe("Alice Admin");
    }

    [Fact]
    public async Task HandleAsync_DefaultsPageSizeAndPage_WhenZeroOrNegative()
    {
        _keycloakMock.GetUsersByRoleAsync("backoffice", "admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateAdmins()));

        var result = await _sut.HandleAsync(new GetPaginatedAdministratorsQuery(0, 0, null, null, null));

        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(20);
    }

    [Fact]
    public async Task HandleAsync_CapsPageSizeAt100()
    {
        _keycloakMock.GetUsersByRoleAsync("backoffice", "admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateAdmins()));

        var result = await _sut.HandleAsync(new GetPaginatedAdministratorsQuery(1, 200, null, null, null));

        result.PageSize.ShouldBe(100);
    }
}