using System.Security.Claims;
using FluentValidation;
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

public class AdminUserControllerExtendedTests
{
    private readonly AdminUserController _sut;
    private readonly IQueryHandler<GetPaginatedCompaniesQuery, PaginatedResult<CompanySummaryDto>> _paginatedCompaniesHandler;
    private readonly IQueryHandler<GetCompanyDetailsQuery, CompanySummaryDto> _companyDetailsHandler;
    private readonly IQueryHandler<GetPaginatedEmployeesQuery, PaginatedResult<EmployeeSummaryDto>> _paginatedEmployeesHandler;
    private readonly IQueryHandler<GetEmployeeDetailsQuery, EmployeeSummaryDto> _employeeDetailsHandler;
    private readonly ICommandHandler<UpdateCompanyCommand, Unit> _updateCompanyHandler;
    private readonly ICommandHandler<DeleteEmployeeCommand, Unit> _deleteEmployeeHandler;
    private readonly ICommandHandler<BlockEmployeeCommand, Unit> _blockEmployeeHandler;
    private readonly ICommandHandler<UnblockEmployeeCommand, Unit> _unblockEmployeeHandler;
    private readonly ICommandHandler<CreateAdminCommand, CreateAdminResult> _createAdminHandler;
    private readonly ICommandHandler<ForcePasswordChangeCommand, Unit> _forcePasswordChangeHandler;
    private readonly IQueryHandler<GetAuditLogQuery, PaginatedResult<AdminAuditLogDto>> _auditLogQueryHandler;
    private readonly IQueryHandler<GetAdministratorsQuery, IReadOnlyList<AdminUserDto>> _administratorsHandler;
    private readonly IQueryHandler<GetPaginatedAdministratorsQuery, PaginatedResult<AdminUserDto>> _paginatedAdministratorsHandler;
    private readonly ICommandHandler<UpdateAdministratorCommand, Unit> _updateAdministratorHandler;
    private readonly ICommandHandler<ResetAdministratorPasswordCommand, ResetAdministratorPasswordResult> _resetAdminPasswordHandler;
    private readonly ICommandHandler<ToggleAdministratorStatusCommand, Unit> _toggleAdminStatusHandler;
    private readonly IKeycloakUserService _keycloakUserService;
    private readonly IValidator<CreateAdminCommand> _createAdminValidator;
    private readonly IValidator<ForcePasswordChangeCommand> _forcePasswordChangeValidator;
    private readonly IValidator<UpdateAdministratorCommand> _updateAdministratorValidator;
    private readonly IValidator<ResetAdministratorPasswordCommand> _resetAdminPasswordValidator;
    private readonly IValidator<ToggleAdministratorStatusCommand> _toggleAdminStatusValidator;

    public AdminUserControllerExtendedTests()
    {
        _paginatedCompaniesHandler = Substitute.For<IQueryHandler<GetPaginatedCompaniesQuery, PaginatedResult<CompanySummaryDto>>>();
        _companyDetailsHandler = Substitute.For<IQueryHandler<GetCompanyDetailsQuery, CompanySummaryDto>>();
        _paginatedEmployeesHandler = Substitute.For<IQueryHandler<GetPaginatedEmployeesQuery, PaginatedResult<EmployeeSummaryDto>>>();
        _employeeDetailsHandler = Substitute.For<IQueryHandler<GetEmployeeDetailsQuery, EmployeeSummaryDto>>();
        _updateCompanyHandler = Substitute.For<ICommandHandler<UpdateCompanyCommand, Unit>>();
        _deleteEmployeeHandler = Substitute.For<ICommandHandler<DeleteEmployeeCommand, Unit>>();
        _blockEmployeeHandler = Substitute.For<ICommandHandler<BlockEmployeeCommand, Unit>>();
        _unblockEmployeeHandler = Substitute.For<ICommandHandler<UnblockEmployeeCommand, Unit>>();
        _createAdminHandler = Substitute.For<ICommandHandler<CreateAdminCommand, CreateAdminResult>>();
        _forcePasswordChangeHandler = Substitute.For<ICommandHandler<ForcePasswordChangeCommand, Unit>>();
        _auditLogQueryHandler = Substitute.For<IQueryHandler<GetAuditLogQuery, PaginatedResult<AdminAuditLogDto>>>();
        _administratorsHandler = Substitute.For<IQueryHandler<GetAdministratorsQuery, IReadOnlyList<AdminUserDto>>>();
        _paginatedAdministratorsHandler = Substitute.For<IQueryHandler<GetPaginatedAdministratorsQuery, PaginatedResult<AdminUserDto>>>();
        _updateAdministratorHandler = Substitute.For<ICommandHandler<UpdateAdministratorCommand, Unit>>();
        _resetAdminPasswordHandler = Substitute.For<ICommandHandler<ResetAdministratorPasswordCommand, ResetAdministratorPasswordResult>>();
        _toggleAdminStatusHandler = Substitute.For<ICommandHandler<ToggleAdministratorStatusCommand, Unit>>();
        _keycloakUserService = Substitute.For<IKeycloakUserService>();
        _createAdminValidator = Substitute.For<IValidator<CreateAdminCommand>>();
        _forcePasswordChangeValidator = Substitute.For<IValidator<ForcePasswordChangeCommand>>();
        _updateAdministratorValidator = Substitute.For<IValidator<UpdateAdministratorCommand>>();
        _resetAdminPasswordValidator = Substitute.For<IValidator<ResetAdministratorPasswordCommand>>();
        _toggleAdminStatusValidator = Substitute.For<IValidator<ToggleAdministratorStatusCommand>>();

        _sut = new AdminUserController(
            _paginatedCompaniesHandler, _companyDetailsHandler, _paginatedEmployeesHandler, _employeeDetailsHandler,
            _updateCompanyHandler, _deleteEmployeeHandler, _blockEmployeeHandler, _unblockEmployeeHandler,
            _createAdminHandler, _forcePasswordChangeHandler, _auditLogQueryHandler, _administratorsHandler,
            _paginatedAdministratorsHandler, _updateAdministratorHandler, _resetAdminPasswordHandler, _toggleAdminStatusHandler,
            _keycloakUserService, _createAdminValidator, _forcePasswordChangeValidator,
            _updateAdministratorValidator, _resetAdminPasswordValidator, _toggleAdminStatusValidator,
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
        _companyDetailsHandler.HandleAsync(Arg.Any<GetCompanyDetailsQuery>(), Arg.Any<CancellationToken>()).Returns(company);

        var response = await _sut.GetCompanyDetails(Guid.NewGuid());

        var okResult = response.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(company);
    }

    [Fact]
    public async Task UpdateCompany_ShouldReturnNoContent_WhenSuccessful()
    {
        _updateCompanyHandler.HandleAsync(Arg.Any<UpdateCompanyCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);

        var response = await _sut.UpdateCompany(Guid.NewGuid(), new UpdateCompanyRequest("Empresa", "emp@test.com", "11999999999"));

        response.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetEmployees_ShouldReturnOk_WhenSuccessful()
    {
        var expected = new PaginatedResult<EmployeeSummaryDto>(new List<EmployeeSummaryDto>(), 0, 1, 20);
        _paginatedEmployeesHandler.HandleAsync(Arg.Any<GetPaginatedEmployeesQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

        var response = await _sut.GetEmployees();

        var okResult = response.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task GetEmployeeDetails_ShouldReturnOk_WhenSuccessful()
    {
        var employee = new EmployeeSummaryDto(Guid.NewGuid(), "João", "52998224725", "joao@test.com", "11999999999", Guid.NewGuid(), "Empresa", Guid.NewGuid(), "Group", false, null);
        _employeeDetailsHandler.HandleAsync(Arg.Any<GetEmployeeDetailsQuery>(), Arg.Any<CancellationToken>()).Returns(employee);

        var response = await _sut.GetEmployeeDetails(Guid.NewGuid());

        var okResult = response.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(employee);
    }

    [Fact]
    public async Task BlockEmployee_ShouldReturnNoContent_WhenSuccessful()
    {
        _blockEmployeeHandler.HandleAsync(Arg.Any<BlockEmployeeCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);

        var response = await _sut.BlockEmployee(Guid.NewGuid());

        response.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UnblockEmployee_ShouldReturnNoContent_WhenSuccessful()
    {
        _unblockEmployeeHandler.HandleAsync(Arg.Any<UnblockEmployeeCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);

        var response = await _sut.UnblockEmployee(Guid.NewGuid());

        response.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteEmployee_ShouldReturnNoContent_WhenSuccessful()
    {
        _deleteEmployeeHandler.HandleAsync(Arg.Any<DeleteEmployeeCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);

        var response = await _sut.DeleteEmployee(Guid.NewGuid());

        response.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CreateAdmin_ShouldReturnConflict_WhenEmailAlreadyExists()
    {
        _createAdminValidator.ValidateAsync(Arg.Any<CreateAdminCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        _createAdminHandler.HandleAsync(Arg.Any<CreateAdminCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("An user with email 'exists@test.com' already exists."));

        var response = await _sut.CreateAdmin(new CreateAdminRequest("Admin", "exists@test.com"));

        var conflict = response.ShouldBeOfType<ConflictObjectResult>();
        conflict.StatusCode.ShouldBe(409);
    }

    [Fact]
    public async Task CreateAdmin_ShouldReturnUnprocessableEntity_WhenValidationFails()
    {
        var validationFailures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("Email", "Invalid email format.")
        };
        _createAdminValidator.ValidateAsync(Arg.Any<CreateAdminCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult(validationFailures));

        var response = await _sut.CreateAdmin(new CreateAdminRequest("Admin", "bad"));

        response.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task GetAdministrators_ShouldReturn503_WhenKeycloakUnavailable()
    {
        _administratorsHandler.HandleAsync(Arg.Any<GetAdministratorsQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var response = await _sut.GetAdministrators();

        var result = response.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(503);
    }

    [Fact]
    public async Task GetAdministratorsPaginated_ShouldReturnOk_WhenSuccessful()
    {
        var expected = new PaginatedResult<AdminUserDto>(new List<AdminUserDto>(), 0, 1, 20);
        _paginatedAdministratorsHandler.HandleAsync(Arg.Any<GetPaginatedAdministratorsQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

        var response = await _sut.GetAdministratorsPaginated();

        var okResult = response.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task GetAdministratorsPaginated_ShouldReturn503_WhenKeycloakUnavailable()
    {
        _paginatedAdministratorsHandler.HandleAsync(Arg.Any<GetPaginatedAdministratorsQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var response = await _sut.GetAdministratorsPaginated();

        var result = response.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(503);
    }

    [Fact]
    public async Task UpdateAdministrator_ShouldReturnNoContent_WhenSuccessful()
    {
        _updateAdministratorValidator.ValidateAsync(Arg.Any<UpdateAdministratorCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        _updateAdministratorHandler.HandleAsync(Arg.Any<UpdateAdministratorCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);

        var response = await _sut.UpdateAdministrator("target-id", new UpdateAdministratorRequest("New Name", "new@test.com"));

        response.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateAdministrator_ShouldReturnNotFound_WhenKeyNotFoundException()
    {
        _updateAdministratorValidator.ValidateAsync(Arg.Any<UpdateAdministratorCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        _updateAdministratorHandler.HandleAsync(Arg.Any<UpdateAdministratorCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException());

        var response = await _sut.UpdateAdministrator("missing-id", new UpdateAdministratorRequest("Name", "email@test.com"));

        var notFound = response.ShouldBeOfType<NotFoundObjectResult>();
        notFound.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task UpdateAdministrator_ShouldReturnConflict_WhenArgumentException()
    {
        _updateAdministratorValidator.ValidateAsync(Arg.Any<UpdateAdministratorCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        _updateAdministratorHandler.HandleAsync(Arg.Any<UpdateAdministratorCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("Email already taken"));

        var response = await _sut.UpdateAdministrator("target-id", new UpdateAdministratorRequest("Name", "taken@test.com"));

        var conflict = response.ShouldBeOfType<ConflictObjectResult>();
        conflict.StatusCode.ShouldBe(409);
    }

    [Fact]
    public async Task UpdateAdministrator_ShouldReturnBadRequest_WhenInvalidOperationException()
    {
        _updateAdministratorValidator.ValidateAsync(Arg.Any<UpdateAdministratorCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        _updateAdministratorHandler.HandleAsync(Arg.Any<UpdateAdministratorCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        var response = await _sut.UpdateAdministrator("target-id", new UpdateAdministratorRequest("Name", "email@test.com"));

        var badRequest = response.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task ResetAdministratorPassword_ShouldReturnOk_WhenSuccessful()
    {
        _resetAdminPasswordValidator.ValidateAsync(Arg.Any<ResetAdministratorPasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        _resetAdminPasswordHandler.HandleAsync(Arg.Any<ResetAdministratorPasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ResetAdministratorPasswordResult("temp-password"));

        var response = await _sut.ResetAdministratorPassword("target-id", new ResetAdministratorPasswordRequest("admin"));

        var okResult = response.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBeOfType<ResetAdministratorPasswordResult>().TemporaryPassword.ShouldBe("temp-password");
    }

    [Fact]
    public async Task ResetAdministratorPassword_ShouldReturnNotFound_WhenKeyNotFoundException()
    {
        _resetAdminPasswordValidator.ValidateAsync(Arg.Any<ResetAdministratorPasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        _resetAdminPasswordHandler.HandleAsync(Arg.Any<ResetAdministratorPasswordCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException());

        var response = await _sut.ResetAdministratorPassword("missing-id", new ResetAdministratorPasswordRequest("admin"));

        var notFound = response.ShouldBeOfType<NotFoundObjectResult>();
        notFound.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task ResetAdministratorPassword_ShouldReturnBadRequest_WhenInvalidOperationException()
    {
        _resetAdminPasswordValidator.ValidateAsync(Arg.Any<ResetAdministratorPasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        _resetAdminPasswordHandler.HandleAsync(Arg.Any<ResetAdministratorPasswordCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Bad operation"));

        var response = await _sut.ResetAdministratorPassword("target-id", new ResetAdministratorPasswordRequest("admin"));

        var badRequest = response.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task ToggleAdministratorStatus_ShouldReturnNoContent_WhenSuccessful()
    {
        _toggleAdminStatusValidator.ValidateAsync(Arg.Any<ToggleAdministratorStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        _toggleAdminStatusHandler.HandleAsync(Arg.Any<ToggleAdministratorStatusCommand>(), Arg.Any<CancellationToken>()).Returns(Unit.Value);

        var response = await _sut.ToggleAdministratorStatus("target-id", new ToggleAdministratorStatusRequest(true, "admin", "reason"));

        response.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ToggleAdministratorStatus_ShouldReturnNotFound_WhenKeyNotFoundException()
    {
        _toggleAdminStatusValidator.ValidateAsync(Arg.Any<ToggleAdministratorStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        _toggleAdminStatusHandler.HandleAsync(Arg.Any<ToggleAdministratorStatusCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException());

        var response = await _sut.ToggleAdministratorStatus("missing-id", new ToggleAdministratorStatusRequest(false, "admin", "reason"));

        var notFound = response.ShouldBeOfType<NotFoundObjectResult>();
        notFound.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task ToggleAdministratorStatus_ShouldReturnBadRequest_WhenInvalidOperationException()
    {
        _toggleAdminStatusValidator.ValidateAsync(Arg.Any<ToggleAdministratorStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        _toggleAdminStatusHandler.HandleAsync(Arg.Any<ToggleAdministratorStatusCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Cannot disable last admin"));

        var response = await _sut.ToggleAdministratorStatus("target-id", new ToggleAdministratorStatusRequest(false, "admin", null));

        var badRequest = response.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task GetAuditLog_ShouldReturnOk_WhenSuccessful()
    {
        var expected = new PaginatedResult<AdminAuditLogDto>(new List<AdminAuditLogDto>(), 0, 1, 20);
        _auditLogQueryHandler.HandleAsync(Arg.Any<GetAuditLogQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

        var response = await _sut.GetAuditLog();

        var okResult = response.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task GetAuditLog_ShouldParseActionTypeFilter()
    {
        var expected = new PaginatedResult<AdminAuditLogDto>(new List<AdminAuditLogDto>(), 0, 1, 20);
        _auditLogQueryHandler.HandleAsync(Arg.Any<GetAuditLogQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

        var response = await _sut.GetAuditLog(actionType: "AdminCreated");

        response.ShouldBeOfType<OkObjectResult>();
        await _auditLogQueryHandler.Received(1).HandleAsync(
            Arg.Is<GetAuditLogQuery>(q => q.ActionType == Onboarding.Domain.Aggregates.Audit.ActionType.AdminCreated),
            Arg.Any<CancellationToken>());
    }
}