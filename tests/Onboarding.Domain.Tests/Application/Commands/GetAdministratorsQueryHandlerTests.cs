using NSubstitute;
using Onboarding.Application.Admin.Queries;
using Onboarding.Application.Common;
using Shouldly;

namespace Onboarding.Domain.Tests.Application.Commands;

[Trait("Category", "Unit")]
public sealed class GetAdministratorsQueryHandlerTests
{
    private readonly IKeycloakUserService _keycloakMock = Substitute.For<IKeycloakUserService>();
    private readonly GetAdministratorsQueryHandler _sut;

    public GetAdministratorsQueryHandlerTests()
        => _sut = new GetAdministratorsQueryHandler(_keycloakMock);

    [Fact]
    public async Task HandleAsync_CallsGetUsersByRoleAsync_WithAdminRole()
    {
        var expected = new List<AdminUserDto>
        {
            new("id1", "admin@test.com", "Admin One", true, false)
        }.AsReadOnly();

        _keycloakMock
            .GetUsersByRoleAsync("admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AdminUserDto>>(expected));

        var result = await _sut.HandleAsync(new GetAdministratorsQuery());

        result.Count.ShouldBe(1);
        result[0].Email.ShouldBe("admin@test.com");
        await _keycloakMock.Received(1).GetUsersByRoleAsync("admin", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenNoAdmins_ReturnsEmptyList()
    {
        _keycloakMock
            .GetUsersByRoleAsync("admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AdminUserDto>>(new List<AdminUserDto>().AsReadOnly()));

        var result = await _sut.HandleAsync(new GetAdministratorsQuery());

        result.ShouldBeEmpty();
    }
}
