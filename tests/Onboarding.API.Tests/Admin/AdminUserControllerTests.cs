using System.Security.Claims;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Onboarding.API.Controllers;
using Onboarding.Application.Admin.Commands;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Admin.Queries;
using Onboarding.Application.Common;
using Shouldly;

namespace Onboarding.API.Tests.Admin;

/// <summary>
/// Unit tests for AdminUserController — refactored for Phase 55 (D-60..D-63).
/// Controller now uses ICommandDispatcher / IQueryDispatcher / IValidationRunner.
/// </summary>
public class AdminUserControllerTests
{
    private readonly ICommandDispatcher _commands = Substitute.For<ICommandDispatcher>();
    private readonly IQueryDispatcher _queries = Substitute.For<IQueryDispatcher>();
    private readonly IValidationRunner _validation = Substitute.For<IValidationRunner>();
    private readonly IKeycloakUserService _keycloakUserService = Substitute.For<IKeycloakUserService>();
    private readonly ILogger<AdminUserController> _logger = Substitute.For<ILogger<AdminUserController>>();

    private readonly AdminUserController _sut;

    public AdminUserControllerTests()
    {
        // Default: validation passes
        _validation.Validate(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _sut = new AdminUserController(
            _commands,
            _queries,
            _validation,
            _keycloakUserService,
            _logger);

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
        _commands.Send<CreateAdminResult>(Arg.Any<CreateAdminCommand>(), Arg.Any<CancellationToken>())
            .Returns(result);

        // Act
        var response = await _sut.CreateAdmin(request);

        // Assert
        var createdResult = response.ShouldBeOfType<CreatedAtActionResult>();
        createdResult.Value.ShouldBe(result);
    }

    [Fact]
    public async Task GetCompanies_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var expected = new PaginatedResult<CompanySummaryDto>(new List<CompanySummaryDto>(), 0, 1, 20);
        _queries.Query<PaginatedResult<CompanySummaryDto>>(Arg.Any<GetPaginatedCompaniesQuery>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var response = await _sut.GetCompanies();

        // Assert
        var okResult = response.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(expected);
    }
}
