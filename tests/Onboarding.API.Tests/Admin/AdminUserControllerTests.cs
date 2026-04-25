using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.API.Controllers;
using Onboarding.Application.Admin.Commands;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Admin.Queries;
using Onboarding.Application.Common;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

public class AdminUserControllerTests
{
    private readonly IQueryHandler<GetPaginatedUsersQuery, PaginatedResult<UserSummaryDto>> _paginatedHandler;
    private readonly ICommandHandler<CreateAdminCommand, CreateAdminResult> _createAdminHandler;
    private readonly IValidator<CreateAdminCommand> _createAdminValidator;
    private readonly AdminUserController _sut;

    public AdminUserControllerTests()
    {
        _paginatedHandler = Substitute.For<IQueryHandler<GetPaginatedUsersQuery, PaginatedResult<UserSummaryDto>>>();
        _createAdminHandler = Substitute.For<ICommandHandler<CreateAdminCommand, CreateAdminResult>>();
        _createAdminValidator = Substitute.For<IValidator<CreateAdminCommand>>();
        
        var logger = Substitute.For<ILogger<AdminUserController>>();

        _sut = new AdminUserController(
            _paginatedHandler,
            Substitute.For<IQueryHandler<GetUserDetailsQuery, UserDetailDto>>(),
            Substitute.For<ICommandHandler<UpdateUserCommand, Unit>>(),
            Substitute.For<ICommandHandler<BlockUserCommand, Unit>>(),
            Substitute.For<ICommandHandler<UnblockUserCommand, Unit>>(),
            Substitute.For<ICommandHandler<DeleteUserCommand, Unit>>(),
            _createAdminHandler,
            Substitute.For<ICommandHandler<ForcePasswordChangeCommand, Unit>>(),
            Substitute.For<IQueryHandler<GetAuditLogQuery, PaginatedResult<AdminAuditLogDto>>>(),
            Substitute.For<IQueryHandler<GetAdministratorsQuery, IReadOnlyList<AdminUserDto>>>(),
            Substitute.For<IQueryHandler<GetPaginatedAdministratorsQuery, PaginatedResult<AdminUserDto>>>(),
            Substitute.For<ICommandHandler<UpdateAdministratorCommand, Unit>>(),
            Substitute.For<ICommandHandler<ResetAdministratorPasswordCommand, ResetAdministratorPasswordResult>>(),
            Substitute.For<ICommandHandler<ToggleAdministratorStatusCommand, Unit>>(),
            Substitute.For<IKeycloakUserService>(),
            Substitute.For<IValidator<UpdateUserCommand>>(),
            Substitute.For<IValidator<DeleteUserCommand>>(),
            _createAdminValidator,
            Substitute.For<IValidator<ForcePasswordChangeCommand>>(),
            Substitute.For<IValidator<UpdateAdministratorCommand>>(),
            Substitute.For<IValidator<ResetAdministratorPasswordCommand>>(),
            Substitute.For<IValidator<ToggleAdministratorStatusCommand>>(),
            logger
        );

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("email", "admin@test.com"),
            new Claim(ClaimTypes.Role, "admin")
        }, "TestAuth"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task CreateAdmin_ShouldReturnCreated_WhenValid()
    {
        // Arrange
        var request = new CreateAdminRequest("New Admin", "new@test.com");
        var result = new CreateAdminResult(Guid.NewGuid(), "temp-pwd");

        _createAdminValidator.ValidateAsync(Arg.Any<CreateAdminCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());

        _createAdminHandler.HandleAsync(Arg.Any<CreateAdminCommand>(), Arg.Any<CancellationToken>())
            .Returns(result);

        // Act
        var response = await _sut.CreateAdmin(request);

        // Assert
        var createdResult = response.ShouldBeOfType<CreatedAtActionResult>();
        createdResult.Value.ShouldBe(result);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var expected = new PaginatedResult<UserSummaryDto>(new List<UserSummaryDto>(), 0, 1, 20);
        _paginatedHandler.HandleAsync(Arg.Any<GetPaginatedUsersQuery>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var response = await _sut.GetUsers();

        // Assert
        var okResult = response.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(expected);
    }
}
