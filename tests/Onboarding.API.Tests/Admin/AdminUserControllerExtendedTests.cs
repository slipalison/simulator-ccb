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
using AdminDeleteEmployeeCommand = Onboarding.Application.Admin.Commands.DeleteEmployeeCommand;

namespace Onboarding.API.Tests.Admin;

/// <summary>
/// Extended unit tests for AdminUserController — Phase 55 refactor (D-60..D-63).
/// Uses ICommandDispatcher + IQueryDispatcher + IValidationRunner.
/// </summary>
public class AdminUserControllerExtendedTests
{
    private readonly ICommandDispatcher _commands = Substitute.For<ICommandDispatcher>();
    private readonly IQueryDispatcher _queries = Substitute.For<IQueryDispatcher>();
    private readonly IValidationRunner _validation = Substitute.For<IValidationRunner>();
    private readonly IKeycloakUserService _keycloakUserService = Substitute.For<IKeycloakUserService>();

    private readonly AdminUserController _sut;

    public AdminUserControllerExtendedTests()
    {
        // Default: validation passes
        _validation.Validate(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _sut = new AdminUserController(
            _commands,
            _queries,
            _validation,
            _keycloakUserService,
            Substitute.For<ILogger<AdminUserController>>());

        SetupUser();
    }

    private void SetupUser()
    {
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
    public async Task GetCompanyDetails_ShouldReturnOk_WhenSuccessful()
    {
        var company = new CompanySummaryDto(Guid.NewGuid(), "Empresa", "11444777000161", "emp@test.com", "11999999999", false, null);
        _queries.Query<CompanySummaryDto>(Arg.Any<GetCompanyDetailsQuery>(), Arg.Any<CancellationToken>())
            .Returns(company);

        var response = await _sut.GetCompanyDetails(Guid.NewGuid());

        var okResult = response.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(company);
    }

    [Fact]
    public async Task UpdateCompany_ShouldReturnNoContent_WhenSuccessful()
    {
        _commands.Send<Unit>(Arg.Any<UpdateCompanyCommand>(), Arg.Any<CancellationToken>())
            .Returns(Unit.Value);

        var response = await _sut.UpdateCompany(Guid.NewGuid(), new UpdateCompanyRequest("Empresa", "emp@test.com", "11999999999"));

        response.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetEmployees_ShouldReturnOk_WhenSuccessful()
    {
        var expected = new PaginatedResult<EmployeeSummaryDto>(new List<EmployeeSummaryDto>(), 0, 1, 20);
        _queries.Query<PaginatedResult<EmployeeSummaryDto>>(Arg.Any<GetPaginatedEmployeesQuery>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var response = await _sut.GetEmployees();

        var okResult = response.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task GetEmployeeDetails_ShouldReturnOk_WhenSuccessful()
    {
        var employee = new EmployeeSummaryDto(Guid.NewGuid(), "João", "52998224725", "joao@test.com", "11999999999", Guid.NewGuid(), "Empresa", Guid.NewGuid(), "Group", false, null);
        _queries.Query<EmployeeSummaryDto>(Arg.Any<GetEmployeeDetailsQuery>(), Arg.Any<CancellationToken>())
            .Returns(employee);

        var response = await _sut.GetEmployeeDetails(Guid.NewGuid());

        var okResult = response.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(employee);
    }

    [Fact]
    public async Task BlockEmployee_ShouldReturnNoContent_WhenSuccessful()
    {
        _commands.Send<Unit>(Arg.Any<BlockEmployeeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Unit.Value);

        var response = await _sut.BlockEmployee(Guid.NewGuid());

        response.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UnblockEmployee_ShouldReturnNoContent_WhenSuccessful()
    {
        _commands.Send<Unit>(Arg.Any<UnblockEmployeeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Unit.Value);

        var response = await _sut.UnblockEmployee(Guid.NewGuid());

        response.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteEmployee_ShouldReturnNoContent_WhenSuccessful()
    {
        _commands.Send<Unit>(Arg.Any<AdminDeleteEmployeeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Unit.Value);

        var response = await _sut.DeleteEmployee(Guid.NewGuid());

        response.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CreateAdmin_ShouldReturnConflict_WhenEmailAlreadyExists()
    {
        _commands.Send<CreateAdminResult>(Arg.Any<CreateAdminCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("An user with email 'exists@test.com' already exists."));

        var response = await _sut.CreateAdmin(new CreateAdminRequest("Admin", "exists@test.com"));

        var conflict = response.ShouldBeOfType<ConflictObjectResult>();
        conflict.StatusCode.ShouldBe(409);
    }

    [Fact]
    public async Task CreateAdmin_ShouldReturnUnprocessableEntity_WhenValidationFails()
    {
        var validationFailures = new List<ValidationFailure>
        {
            new("Email", "Invalid email format.")
        };
        _validation.Validate(Arg.Any<CreateAdminCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(validationFailures));

        var response = await _sut.CreateAdmin(new CreateAdminRequest("Admin", "bad"));

        response.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task GetAdministrators_ShouldReturn503_WhenKeycloakUnavailable()
    {
        _queries.Query<IReadOnlyList<AdminUserDto>>(Arg.Any<GetAdministratorsQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var response = await _sut.GetAdministrators();

        var result = response.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(503);
    }

    [Fact]
    public async Task GetAdministratorsPaginated_ShouldReturnOk_WhenSuccessful()
    {
        var expected = new PaginatedResult<AdminUserDto>(new List<AdminUserDto>(), 0, 1, 20);
        _queries.Query<PaginatedResult<AdminUserDto>>(Arg.Any<GetPaginatedAdministratorsQuery>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var response = await _sut.GetAdministratorsPaginated();

        var okResult = response.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task GetAdministratorsPaginated_ShouldReturn503_WhenKeycloakUnavailable()
    {
        _queries.Query<PaginatedResult<AdminUserDto>>(Arg.Any<GetPaginatedAdministratorsQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var response = await _sut.GetAdministratorsPaginated();

        var result = response.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(503);
    }

    [Fact]
    public async Task UpdateAdministrator_ShouldReturnNoContent_WhenSuccessful()
    {
        _commands.Send<Unit>(Arg.Any<UpdateAdministratorCommand>(), Arg.Any<CancellationToken>())
            .Returns(Unit.Value);

        var response = await _sut.UpdateAdministrator("target-id", new UpdateAdministratorRequest("New Name", "new@test.com"));

        response.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateAdministrator_ShouldReturnNotFound_WhenKeyNotFoundException()
    {
        _commands.Send<Unit>(Arg.Any<UpdateAdministratorCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException());

        var response = await _sut.UpdateAdministrator("missing-id", new UpdateAdministratorRequest("Name", "email@test.com"));

        var notFound = response.ShouldBeOfType<NotFoundObjectResult>();
        notFound.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task UpdateAdministrator_ShouldReturnConflict_WhenArgumentException()
    {
        _commands.Send<Unit>(Arg.Any<UpdateAdministratorCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("Email already taken"));

        var response = await _sut.UpdateAdministrator("target-id", new UpdateAdministratorRequest("Name", "taken@test.com"));

        var conflict = response.ShouldBeOfType<ConflictObjectResult>();
        conflict.StatusCode.ShouldBe(409);
    }

    [Fact]
    public async Task UpdateAdministrator_ShouldReturnBadRequest_WhenInvalidOperationException()
    {
        _commands.Send<Unit>(Arg.Any<UpdateAdministratorCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        var response = await _sut.UpdateAdministrator("target-id", new UpdateAdministratorRequest("Name", "email@test.com"));

        var badRequest = response.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task ResetAdministratorPassword_ShouldReturnOk_WhenSuccessful()
    {
        _commands.Send<ResetAdministratorPasswordResult>(Arg.Any<ResetAdministratorPasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ResetAdministratorPasswordResult("temp-password"));

        var response = await _sut.ResetAdministratorPassword("target-id", new ResetAdministratorPasswordRequest("admin"));

        var okResult = response.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBeOfType<ResetAdministratorPasswordResult>().TemporaryPassword.ShouldBe("temp-password");
    }

    [Fact]
    public async Task ResetAdministratorPassword_ShouldReturnNotFound_WhenKeyNotFoundException()
    {
        _commands.Send<ResetAdministratorPasswordResult>(Arg.Any<ResetAdministratorPasswordCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException());

        var response = await _sut.ResetAdministratorPassword("missing-id", new ResetAdministratorPasswordRequest("admin"));

        var notFound = response.ShouldBeOfType<NotFoundObjectResult>();
        notFound.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task ResetAdministratorPassword_ShouldReturnBadRequest_WhenInvalidOperationException()
    {
        _commands.Send<ResetAdministratorPasswordResult>(Arg.Any<ResetAdministratorPasswordCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Bad operation"));

        var response = await _sut.ResetAdministratorPassword("target-id", new ResetAdministratorPasswordRequest("admin"));

        var badRequest = response.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task ToggleAdministratorStatus_ShouldReturnNoContent_WhenSuccessful()
    {
        _commands.Send<Unit>(Arg.Any<ToggleAdministratorStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(Unit.Value);

        var response = await _sut.ToggleAdministratorStatus("target-id", new ToggleAdministratorStatusRequest(true, "admin", "reason"));

        response.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ToggleAdministratorStatus_ShouldReturnNotFound_WhenKeyNotFoundException()
    {
        _commands.Send<Unit>(Arg.Any<ToggleAdministratorStatusCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException());

        var response = await _sut.ToggleAdministratorStatus("missing-id", new ToggleAdministratorStatusRequest(false, "admin", "reason"));

        var notFound = response.ShouldBeOfType<NotFoundObjectResult>();
        notFound.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task ToggleAdministratorStatus_ShouldReturnBadRequest_WhenInvalidOperationException()
    {
        _commands.Send<Unit>(Arg.Any<ToggleAdministratorStatusCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Cannot disable last admin"));

        var response = await _sut.ToggleAdministratorStatus("target-id", new ToggleAdministratorStatusRequest(false, "admin", null));

        var badRequest = response.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task GetAuditLog_ShouldReturnOk_WhenSuccessful()
    {
        var expected = new PaginatedResult<AdminAuditLogDto>(new List<AdminAuditLogDto>(), 0, 1, 20);
        _queries.Query<PaginatedResult<AdminAuditLogDto>>(Arg.Any<GetAuditLogQuery>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var response = await _sut.GetAuditLog();

        var okResult = response.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task GetAuditLog_ShouldParseActionTypeFilter()
    {
        var expected = new PaginatedResult<AdminAuditLogDto>(new List<AdminAuditLogDto>(), 0, 1, 20);
        GetAuditLogQuery? captured = null;
        _queries.Query<PaginatedResult<AdminAuditLogDto>>(
            Arg.Do<object>(q => captured = q as GetAuditLogQuery),
            Arg.Any<CancellationToken>())
            .Returns(expected);

        var response = await _sut.GetAuditLog(actionType: "AdminCreated");

        response.ShouldBeOfType<OkObjectResult>();
        captured.ShouldNotBeNull();
        captured!.ActionType.ShouldBe(Onboarding.Domain.Aggregates.Audit.ActionType.AdminCreated);
    }
}
