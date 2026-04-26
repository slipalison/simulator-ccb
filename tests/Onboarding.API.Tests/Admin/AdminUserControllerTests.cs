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
    private readonly IQueryHandler<GetPaginatedCompaniesQuery, PaginatedResult<CompanySummaryDto>> _paginatedCompaniesHandler;
    private readonly ICommandHandler<CreateAdminCommand, CreateAdminResult> _createAdminHandler;
    private readonly IValidator<CreateAdminCommand> _createAdminValidator;
    private readonly AdminUserController _sut;

    public AdminUserControllerTests()
    {
        _paginatedCompaniesHandler = Substitute.For<IQueryHandler<GetPaginatedCompaniesQuery, PaginatedResult<CompanySummaryDto>>>();
        _createAdminHandler = Substitute.For<ICommandHandler<CreateAdminCommand, CreateAdminResult>>();
        _createAdminValidator = Substitute.For<IValidator<CreateAdminCommand>>();

        var logger = Substitute.For<ILogger<AdminUserController>>();

        _sut = new AdminUserController(
            // Company/Employee handlers (Phase 37)
            _paginatedCompaniesHandler,
            Substitute.For<IQueryHandler<GetCompanyDetailsQuery, CompanySummaryDto>>(),
            Substitute.For<IQueryHandler<GetPaginatedEmployeesQuery, PaginatedResult<EmployeeSummaryDto>>>(),
            Substitute.For<IQueryHandler<GetEmployeeDetailsQuery, EmployeeSummaryDto>>(),
            Substitute.For<ICommandHandler<UpdateCompanyCommand, Unit>>(),
            Substitute.For<ICommandHandler<DeleteEmployeeCommand, Unit>>(),
            Substitute.For<ICommandHandler<BlockEmployeeCommand, Unit>>(),
            Substitute.For<ICommandHandler<UnblockEmployeeCommand, Unit>>(),
            // Phase 29 — Admin management
            _createAdminHandler,
            Substitute.For<ICommandHandler<ForcePasswordChangeCommand, Unit>>(),
            Substitute.For<IQueryHandler<GetAuditLogQuery, PaginatedResult<AdminAuditLogDto>>>(),
            Substitute.For<IQueryHandler<GetAdministratorsQuery, IReadOnlyList<AdminUserDto>>>(),
            // Phase 35 — Admin management
            Substitute.For<IQueryHandler<GetPaginatedAdministratorsQuery, PaginatedResult<AdminUserDto>>>(),
            Substitute.For<ICommandHandler<UpdateAdministratorCommand, Unit>>(),
            Substitute.For<ICommandHandler<ResetAdministratorPasswordCommand, ResetAdministratorPasswordResult>>(),
            Substitute.For<ICommandHandler<ToggleAdministratorStatusCommand, Unit>>(),
            Substitute.For<IKeycloakUserService>(),
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
    public async Task GetCompanies_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var expected = new PaginatedResult<CompanySummaryDto>(new List<CompanySummaryDto>(), 0, 1, 20);
        _paginatedCompaniesHandler.HandleAsync(Arg.Any<GetPaginatedCompaniesQuery>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var response = await _sut.GetCompanies();

        // Assert
        var okResult = response.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(expected);
    }
}