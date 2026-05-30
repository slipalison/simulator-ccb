using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Onboarding.API.Controllers;
using Onboarding.Application.Common;
using Onboarding.Application.Companies.Commands;
using Onboarding.Application.Companies.DTOs;
using Onboarding.Application.Companies.Queries;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.API.Tests.Controllers;

/// <summary>
/// Unit tests for CompaniesController — covers all endpoints:
/// GET /me, POST /registration, POST/GET/PUT/DELETE employees,
/// POST/GET/PUT/DELETE access-groups, toggle-status, reset-password, change-access-group.
///
/// Strategy: direct controller instantiation with mocked dependencies (no WebApplicationFactory).
/// HTTP context is set via ControllerContext with a fake ClaimsPrincipal for auth-required paths.
/// Company isolation (companyId mismatch → 403) tested on every guarded endpoint.
/// </summary>
[Trait("Category", "Unit")]
public sealed class CompaniesControllerTests
{
    // =========================================================================
    // Mocks
    // =========================================================================

    private readonly ICompanyRepository _companyRepo = Substitute.For<ICompanyRepository>();
    private readonly ICommandHandler<RegisterCompanyCommand, RegisterCompanyResult> _registerCompanyHandler =
        Substitute.For<ICommandHandler<RegisterCompanyCommand, RegisterCompanyResult>>();
    private readonly IValidator<RegisterCompanyCommand> _registerCompanyValidator =
        Substitute.For<IValidator<RegisterCompanyCommand>>();
    private readonly ICommandHandler<RegisterEmployeeCommand, RegisterEmployeeResult> _registerEmployeeHandler =
        Substitute.For<ICommandHandler<RegisterEmployeeCommand, RegisterEmployeeResult>>();
    private readonly IValidator<RegisterEmployeeCommand> _registerEmployeeValidator =
        Substitute.For<IValidator<RegisterEmployeeCommand>>();
    private readonly IQueryHandler<GetCompanyEmployeesQuery, PaginatedResult<EmployeeListItemDto>> _getEmployeesHandler =
        Substitute.For<IQueryHandler<GetCompanyEmployeesQuery, PaginatedResult<EmployeeListItemDto>>>();
    private readonly ICommandHandler<ToggleEmployeeStatusCommand, Unit> _toggleStatusHandler =
        Substitute.For<ICommandHandler<ToggleEmployeeStatusCommand, Unit>>();
    private readonly ICommandHandler<ResetEmployeePasswordCommand, ResetEmployeePasswordResult> _resetPasswordHandler =
        Substitute.For<ICommandHandler<ResetEmployeePasswordCommand, ResetEmployeePasswordResult>>();
    private readonly ICommandHandler<UpdateEmployeeCommand, Unit> _updateEmployeeHandler =
        Substitute.For<ICommandHandler<UpdateEmployeeCommand, Unit>>();
    private readonly ICommandHandler<DeleteEmployeeCommand, Unit> _deleteEmployeeHandler =
        Substitute.For<ICommandHandler<DeleteEmployeeCommand, Unit>>();
    private readonly ICommandHandler<ChangeEmployeeAccessGroupCommand, Unit> _changeAccessGroupHandler =
        Substitute.For<ICommandHandler<ChangeEmployeeAccessGroupCommand, Unit>>();
    private readonly ICommandHandler<CreateAccessGroupCommand, AccessGroupDto> _createAccessGroupHandler =
        Substitute.For<ICommandHandler<CreateAccessGroupCommand, AccessGroupDto>>();
    private readonly ICommandHandler<UpdateAccessGroupCommand, AccessGroupDto> _updateAccessGroupHandler =
        Substitute.For<ICommandHandler<UpdateAccessGroupCommand, AccessGroupDto>>();
    private readonly ICommandHandler<DeleteAccessGroupCommand, Unit> _deleteAccessGroupHandler =
        Substitute.For<ICommandHandler<DeleteAccessGroupCommand, Unit>>();
    private readonly ICurrentCompanyService _currentCompanyService = Substitute.For<ICurrentCompanyService>();
    private readonly IAccessGroupRepository _accessGroupRepo = Substitute.For<IAccessGroupRepository>();
    private readonly ILogger<CompaniesController> _logger = Substitute.For<ILogger<CompaniesController>>();

    private static readonly Guid CompanyId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private const string ActorSub = "actor-sub-001";
    private const string ActorEmail = "actor@company.com";

    // =========================================================================
    // Helpers
    // =========================================================================

    private CompaniesController BuildSut(Guid? currentCompanyId = null, bool includeAuthClaims = true)
    {
        _currentCompanyService.CompanyId.Returns(currentCompanyId ?? CompanyId);

        var controller = new CompaniesController(
            _companyRepo,
            _registerCompanyHandler,
            _registerCompanyValidator,
            _registerEmployeeHandler,
            _registerEmployeeValidator,
            _getEmployeesHandler,
            _toggleStatusHandler,
            _resetPasswordHandler,
            _updateEmployeeHandler,
            _deleteEmployeeHandler,
            _changeAccessGroupHandler,
            _createAccessGroupHandler,
            _updateAccessGroupHandler,
            _deleteAccessGroupHandler,
            _currentCompanyService,
            _accessGroupRepo,
            _logger);

        var claims = new List<Claim>();
        if (includeAuthClaims)
        {
            claims.Add(new Claim("sub", ActorSub));
            claims.Add(new Claim("email", ActorEmail));
        }

        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return controller;
    }

    private static ValidationResult PassValidation() => new();

    private static ValidationResult FailValidation(string property = "Field", string error = "Required")
        => new(new[] { new ValidationFailure(property, error) });

    // =========================================================================
    // GET /api/companies/me
    // =========================================================================

    [Fact]
    public async Task GetMe_WhenCompanyFoundBySub_Returns200WithProfile()
    {
        // Arrange
        var sut = BuildSut();
        var terms = TermsAcceptance.Create("1.0", "127.0.0.1");
        var company = Company.Register("ACME Ltda", "11222333000181", "acme@test.com", "11999999999", terms);
        _companyRepo.GetByKeycloakSubAsync(ActorSub, Arg.Any<CancellationToken>())
            .Returns(company);

        // Act
        var result = await sut.GetMe(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        ok.Value.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetMe_WhenCompanyNotFoundBySub_AndEmployeeCompanyFound_Returns200()
    {
        // Arrange — employee path: sub doesn't match company, but CompanyId is set
        var sut = BuildSut();
        _companyRepo.GetByKeycloakSubAsync(ActorSub, Arg.Any<CancellationToken>())
            .Returns((Company?)null);
        var terms = TermsAcceptance.Create("1.0", "127.0.0.1");
        var company = Company.Register("Employee Corp", "11222333000181", "corp@test.com", "11988888888", terms);
        _companyRepo.GetByIdAsync(CompanyId, Arg.Any<CancellationToken>())
            .Returns(company);

        // Act
        var result = await sut.GetMe(CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkObjectResult>().StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task GetMe_WhenNeitherCompanyNorEmployeeFound_Returns404()
    {
        // Arrange — both lookups miss
        var sut = BuildSut(currentCompanyId: Guid.Empty);
        _companyRepo.GetByKeycloakSubAsync(ActorSub, Arg.Any<CancellationToken>())
            .Returns((Company?)null);

        // Act
        var result = await sut.GetMe(CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetMe_WhenMissingSubClaim_Returns401()
    {
        // Arrange — no sub claim
        var sut = BuildSut(includeAuthClaims: false);

        // Act
        var result = await sut.GetMe(CancellationToken.None);

        // Assert
        result.ShouldBeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetMe_WhenCompanyNotFoundBySubAndCompanyIdLookupMisses_Returns404()
    {
        // Arrange — sub miss, companyId set but DB miss too
        var sut = BuildSut();
        _companyRepo.GetByKeycloakSubAsync(ActorSub, Arg.Any<CancellationToken>())
            .Returns((Company?)null);
        _companyRepo.GetByIdAsync(CompanyId, Arg.Any<CancellationToken>())
            .Returns((Company?)null);

        // Act
        var result = await sut.GetMe(CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    // =========================================================================
    // POST /api/companies/registration
    // =========================================================================

    [Fact]
    public async Task RegisterCompany_WhenNullBody_Returns400()
    {
        // Arrange
        var sut = BuildSut();

        // Act
        var result = await sut.RegisterCompany(null, CancellationToken.None);

        // Assert
        var bad = result.ShouldBeOfType<BadRequestObjectResult>();
        bad.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task RegisterCompany_WhenValidationFails_Returns422()
    {
        // Arrange
        var sut = BuildSut();
        _registerCompanyValidator
            .ValidateAsync(Arg.Any<RegisterCompanyCommand>(), Arg.Any<CancellationToken>())
            .Returns(FailValidation("RazaoSocial", "Required"));

        var request = new RegisterCompanyRequest("", "", "", "", "", false);

        // Act
        var result = await sut.RegisterCompany(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<UnprocessableEntityObjectResult>().StatusCode.ShouldBe(422);
    }

    [Fact]
    public async Task RegisterCompany_WhenDuplicateCompany_Returns409()
    {
        // Arrange
        var sut = BuildSut();
        _registerCompanyValidator
            .ValidateAsync(Arg.Any<RegisterCompanyCommand>(), Arg.Any<CancellationToken>())
            .Returns(PassValidation());
        _registerCompanyHandler
            .HandleAsync(Arg.Any<RegisterCompanyCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateCompanyException("CNPJ already registered."));

        var request = new RegisterCompanyRequest("Empresa X", "11222333000181", "e@test.com", "11999999999", "S3cr3t!", true);

        // Act
        var result = await sut.RegisterCompany(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ConflictObjectResult>().StatusCode.ShouldBe(409);
    }

    [Fact]
    public async Task RegisterCompany_WhenDuplicateKeycloakUser_Returns409()
    {
        // Arrange
        var sut = BuildSut();
        _registerCompanyValidator
            .ValidateAsync(Arg.Any<RegisterCompanyCommand>(), Arg.Any<CancellationToken>())
            .Returns(PassValidation());
        _registerCompanyHandler
            .HandleAsync(Arg.Any<RegisterCompanyCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateKeycloakUserException("Email already in use."));

        var request = new RegisterCompanyRequest("Empresa X", "11222333000181", "e@test.com", "11999999999", "S3cr3t!", true);

        // Act
        var result = await sut.RegisterCompany(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ConflictObjectResult>().StatusCode.ShouldBe(409);
    }

    [Fact]
    public async Task RegisterCompany_WhenSuccess_Returns201()
    {
        // Arrange
        var sut = BuildSut();
        _registerCompanyValidator
            .ValidateAsync(Arg.Any<RegisterCompanyCommand>(), Arg.Any<CancellationToken>())
            .Returns(PassValidation());
        var registrationResult = new RegisterCompanyResult(Guid.NewGuid(), "kc-user-001");
        _registerCompanyHandler
            .HandleAsync(Arg.Any<RegisterCompanyCommand>(), Arg.Any<CancellationToken>())
            .Returns(registrationResult);

        var request = new RegisterCompanyRequest("Empresa X", "11222333000181", "e@test.com", "11999999999", "S3cr3t!", true);

        // Act
        var result = await sut.RegisterCompany(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<CreatedAtActionResult>().StatusCode.ShouldBe(201);
    }

    [Fact]
    public async Task RegisterCompany_WithXForwardedFor_ResolvesClientIpFromHeader()
    {
        // Arrange — exercise the X-Forwarded-For header path in ResolveClientIpAddress
        var sut = BuildSut();
        sut.ControllerContext.HttpContext.Request.Headers["X-Forwarded-For"] = "192.168.1.1, 10.0.0.1";
        _registerCompanyValidator
            .ValidateAsync(Arg.Any<RegisterCompanyCommand>(), Arg.Any<CancellationToken>())
            .Returns(PassValidation());
        _registerCompanyHandler
            .HandleAsync(Arg.Any<RegisterCompanyCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RegisterCompanyResult(Guid.NewGuid(), "kc-002"));

        var request = new RegisterCompanyRequest("Empresa Y", "22333444000195", "y@test.com", "11988888888", "S3cr3t!", true);

        // Act — verifies no exception is thrown and 201 is returned
        var result = await sut.RegisterCompany(request, CancellationToken.None);

        result.ShouldBeOfType<CreatedAtActionResult>().StatusCode.ShouldBe(201);
    }

    // =========================================================================
    // POST /api/companies/{companyId}/employees
    // =========================================================================

    [Fact]
    public async Task RegisterEmployee_WhenCompanyIdMismatch_Returns403()
    {
        // Arrange — route companyId differs from currentCompanyService.CompanyId
        var sut = BuildSut();
        var differentCompanyId = Guid.NewGuid();

        // Act
        var result = await sut.RegisterEmployee(differentCompanyId, new RegisterEmployeeRequest("Jo", "12345678909", "jo@test.com", "11999999999", null), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task RegisterEmployee_WhenNullBody_Returns400()
    {
        // Arrange
        var sut = BuildSut();

        // Act
        var result = await sut.RegisterEmployee(CompanyId, null, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>().StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task RegisterEmployee_WhenValidationFails_Returns422()
    {
        // Arrange
        var sut = BuildSut();
        _registerEmployeeValidator
            .ValidateAsync(Arg.Any<RegisterEmployeeCommand>(), Arg.Any<CancellationToken>())
            .Returns(FailValidation("Cpf", "Invalid"));

        var request = new RegisterEmployeeRequest("", "", "", "", null);

        // Act
        var result = await sut.RegisterEmployee(CompanyId, request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<UnprocessableEntityObjectResult>().StatusCode.ShouldBe(422);
    }

    [Fact]
    public async Task RegisterEmployee_WhenBadRequestException_Returns400()
    {
        // Arrange
        var sut = BuildSut();
        _registerEmployeeValidator
            .ValidateAsync(Arg.Any<RegisterEmployeeCommand>(), Arg.Any<CancellationToken>())
            .Returns(PassValidation());
        _registerEmployeeHandler
            .HandleAsync(Arg.Any<RegisterEmployeeCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new BadRequestException("Access group not found."));

        var request = new RegisterEmployeeRequest("João", "12345678909", "jo@test.com", "11999999999", Guid.NewGuid());

        // Act
        var result = await sut.RegisterEmployee(CompanyId, request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>().StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task RegisterEmployee_WhenDuplicateKeycloakUser_Returns409()
    {
        // Arrange
        var sut = BuildSut();
        _registerEmployeeValidator
            .ValidateAsync(Arg.Any<RegisterEmployeeCommand>(), Arg.Any<CancellationToken>())
            .Returns(PassValidation());
        _registerEmployeeHandler
            .HandleAsync(Arg.Any<RegisterEmployeeCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateKeycloakUserException("Email in use."));

        var request = new RegisterEmployeeRequest("João", "12345678909", "jo@test.com", "11999999999", null);

        // Act
        var result = await sut.RegisterEmployee(CompanyId, request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ConflictObjectResult>().StatusCode.ShouldBe(409);
    }

    [Fact]
    public async Task RegisterEmployee_WhenSuccess_Returns201()
    {
        // Arrange
        var sut = BuildSut();
        _registerEmployeeValidator
            .ValidateAsync(Arg.Any<RegisterEmployeeCommand>(), Arg.Any<CancellationToken>())
            .Returns(PassValidation());
        _registerEmployeeHandler
            .HandleAsync(Arg.Any<RegisterEmployeeCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RegisterEmployeeResult(Guid.NewGuid(), "Temp@1234"));

        var request = new RegisterEmployeeRequest("João", "12345678909", "jo@test.com", "11999999999", null);

        // Act
        var result = await sut.RegisterEmployee(CompanyId, request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<CreatedAtActionResult>().StatusCode.ShouldBe(201);
    }

    // =========================================================================
    // GET /api/companies/{companyId}/employees
    // =========================================================================

    [Fact]
    public async Task GetEmployees_WhenCompanyIdMismatch_Returns403()
    {
        // Arrange
        var sut = BuildSut();

        // Act
        var result = await sut.GetEmployees(Guid.NewGuid(), ct: CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetEmployees_WhenSuccess_Returns200WithPaginatedResult()
    {
        // Arrange
        var sut = BuildSut();
        var paged = new PaginatedResult<EmployeeListItemDto>([], 0, 1, 20);
        _getEmployeesHandler
            .HandleAsync(Arg.Any<GetCompanyEmployeesQuery>(), Arg.Any<CancellationToken>())
            .Returns(paged);

        // Act
        var result = await sut.GetEmployees(CompanyId, ct: CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        ok.Value.ShouldBe(paged);
    }

    [Fact]
    public async Task GetEmployees_WhenSuccessWithSearchAndStatus_Returns200()
    {
        // Arrange — exercise optional query params path
        var sut = BuildSut();
        _getEmployeesHandler
            .HandleAsync(Arg.Any<GetCompanyEmployeesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<EmployeeListItemDto>([], 0, 1, 20));

        // Act
        var result = await sut.GetEmployees(CompanyId, page: 2, pageSize: 10, search: "João", status: "active", ct: CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkObjectResult>().StatusCode.ShouldBe(200);
        await _getEmployeesHandler.Received(1)
            .HandleAsync(Arg.Is<GetCompanyEmployeesQuery>(q =>
                q.Page == 2 && q.PageSize == 10 && q.Search == "João" && q.Status == "active"),
                Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // POST /api/companies/{companyId}/employees/{id}/toggle-status
    // =========================================================================

    [Fact]
    public async Task ToggleEmployeeStatus_WhenCompanyIdMismatch_Returns403()
    {
        // Arrange
        var sut = BuildSut();

        // Act
        var result = await sut.ToggleEmployeeStatus(Guid.NewGuid(), Guid.NewGuid(), new ToggleStatusRequest(true), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ToggleEmployeeStatus_WhenSuccess_Returns200()
    {
        // Arrange
        var sut = BuildSut();
        _toggleStatusHandler
            .HandleAsync(Arg.Any<ToggleEmployeeStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(Unit.Value);

        // Act
        var result = await sut.ToggleEmployeeStatus(CompanyId, Guid.NewGuid(), new ToggleStatusRequest(true), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>().StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task ToggleEmployeeStatus_WhenNotFound_Returns404()
    {
        // Arrange
        var sut = BuildSut();
        _toggleStatusHandler
            .HandleAsync(Arg.Any<ToggleEmployeeStatusCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Employee not found."));

        // Act
        var result = await sut.ToggleEmployeeStatus(CompanyId, Guid.NewGuid(), new ToggleStatusRequest(false), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ToggleEmployeeStatus_WithDeactivate_PassesActivateFalseToCommand()
    {
        // Arrange — exercises ToggleStatusRequest(Activate=false) branch
        var sut = BuildSut();
        _toggleStatusHandler
            .HandleAsync(Arg.Any<ToggleEmployeeStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(Unit.Value);
        var employeeId = Guid.NewGuid();

        // Act
        var result = await sut.ToggleEmployeeStatus(CompanyId, employeeId, new ToggleStatusRequest(false), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
        await _toggleStatusHandler.Received(1)
            .HandleAsync(Arg.Is<ToggleEmployeeStatusCommand>(c => !c.Activate), Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // POST /api/companies/{companyId}/employees/{id}/reset-password
    // =========================================================================

    [Fact]
    public async Task ResetEmployeePassword_WhenCompanyIdMismatch_Returns403()
    {
        // Arrange
        var sut = BuildSut();

        // Act
        var result = await sut.ResetEmployeePassword(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ResetEmployeePassword_WhenSuccess_Returns200WithTempPassword()
    {
        // Arrange
        var sut = BuildSut();
        _resetPasswordHandler
            .HandleAsync(Arg.Any<ResetEmployeePasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ResetEmployeePasswordResult("Temp@5678"));

        // Act
        var result = await sut.ResetEmployeePassword(CompanyId, Guid.NewGuid(), CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        ok.Value.ShouldBeOfType<ResetEmployeePasswordResult>()
            .TemporaryPassword.ShouldBe("Temp@5678");
    }

    [Fact]
    public async Task ResetEmployeePassword_WhenNotFound_Returns404()
    {
        // Arrange
        var sut = BuildSut();
        _resetPasswordHandler
            .HandleAsync(Arg.Any<ResetEmployeePasswordCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Employee not found."));

        // Act
        var result = await sut.ResetEmployeePassword(CompanyId, Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    // =========================================================================
    // PUT /api/companies/{companyId}/employees/{id}
    // =========================================================================

    [Fact]
    public async Task UpdateEmployee_WhenCompanyIdMismatch_Returns403()
    {
        // Arrange
        var sut = BuildSut();

        // Act
        var result = await sut.UpdateEmployee(Guid.NewGuid(), Guid.NewGuid(), new UpdateEmployeeRequest("Name", "e@t.com", "119"), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateEmployee_WhenSuccess_Returns200()
    {
        // Arrange
        var sut = BuildSut();
        _updateEmployeeHandler
            .HandleAsync(Arg.Any<UpdateEmployeeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Unit.Value);

        // Act
        var result = await sut.UpdateEmployee(CompanyId, Guid.NewGuid(), new UpdateEmployeeRequest("João Updated", "jo@test.com", "11999999999"), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>().StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task UpdateEmployee_WhenNotFound_Returns404()
    {
        // Arrange
        var sut = BuildSut();
        _updateEmployeeHandler
            .HandleAsync(Arg.Any<UpdateEmployeeCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Employee not found."));

        // Act
        var result = await sut.UpdateEmployee(CompanyId, Guid.NewGuid(), new UpdateEmployeeRequest("João", "jo@test.com", "11999999999"), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateEmployee_WithNullFieldsInRequest_PassesEmptyStringsToCommand()
    {
        // Arrange — exercises nullable fields via UpdateEmployeeRequest(null, null, null) → string.Empty
        var sut = BuildSut();
        _updateEmployeeHandler
            .HandleAsync(Arg.Any<UpdateEmployeeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Unit.Value);

        // Act
        var result = await sut.UpdateEmployee(CompanyId, Guid.NewGuid(), new UpdateEmployeeRequest(null, null, null), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
        await _updateEmployeeHandler.Received(1)
            .HandleAsync(Arg.Is<UpdateEmployeeCommand>(c =>
                c.Nome == string.Empty && c.Email == string.Empty && c.Phone == string.Empty),
                Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // DELETE /api/companies/{companyId}/employees/{id}
    // =========================================================================

    [Fact]
    public async Task DeleteEmployee_WhenCompanyIdMismatch_Returns403()
    {
        // Arrange
        var sut = BuildSut();

        // Act
        var result = await sut.DeleteEmployee(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteEmployee_WhenSuccess_Returns200()
    {
        // Arrange
        var sut = BuildSut();
        _deleteEmployeeHandler
            .HandleAsync(Arg.Any<DeleteEmployeeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Unit.Value);

        // Act
        var result = await sut.DeleteEmployee(CompanyId, Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>().StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task DeleteEmployee_WhenNotFound_Returns404()
    {
        // Arrange
        var sut = BuildSut();
        _deleteEmployeeHandler
            .HandleAsync(Arg.Any<DeleteEmployeeCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Employee not found."));

        // Act
        var result = await sut.DeleteEmployee(CompanyId, Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    // =========================================================================
    // PUT /api/companies/{companyId}/employees/{id}/access-group
    // =========================================================================

    [Fact]
    public async Task ChangeEmployeeAccessGroup_WhenCompanyIdMismatch_Returns403()
    {
        // Arrange
        var sut = BuildSut();

        // Act
        var result = await sut.ChangeEmployeeAccessGroup(Guid.NewGuid(), Guid.NewGuid(), new ChangeAccessGroupRequest(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ChangeEmployeeAccessGroup_WhenSuccess_Returns200()
    {
        // Arrange
        var sut = BuildSut();
        _changeAccessGroupHandler
            .HandleAsync(Arg.Any<ChangeEmployeeAccessGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Unit.Value);

        // Act
        var result = await sut.ChangeEmployeeAccessGroup(CompanyId, Guid.NewGuid(), new ChangeAccessGroupRequest(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>().StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task ChangeEmployeeAccessGroup_WhenNotFound_Returns404()
    {
        // Arrange
        var sut = BuildSut();
        _changeAccessGroupHandler
            .HandleAsync(Arg.Any<ChangeEmployeeAccessGroupCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Employee or access group not found."));

        // Act
        var result = await sut.ChangeEmployeeAccessGroup(CompanyId, Guid.NewGuid(), new ChangeAccessGroupRequest(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ChangeEmployeeAccessGroup_PassesCorrectAccessGroupIdToCommand()
    {
        // Arrange — exercises ChangeAccessGroupRequest.AccessGroupId wiring
        var sut = BuildSut();
        _changeAccessGroupHandler
            .HandleAsync(Arg.Any<ChangeEmployeeAccessGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Unit.Value);
        var newGroupId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        // Act
        await sut.ChangeEmployeeAccessGroup(CompanyId, employeeId, new ChangeAccessGroupRequest(newGroupId), CancellationToken.None);

        // Assert
        await _changeAccessGroupHandler.Received(1)
            .HandleAsync(Arg.Is<ChangeEmployeeAccessGroupCommand>(c =>
                c.NewAccessGroupId == newGroupId && c.EmployeeId == employeeId),
                Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // GET /api/companies/{companyId}/access-groups
    // =========================================================================

    [Fact]
    public async Task GetAccessGroups_WhenCompanyIdMismatch_Returns403()
    {
        // Arrange
        var sut = BuildSut();

        // Act
        var result = await sut.GetAccessGroups(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetAccessGroups_WhenSuccess_Returns200WithGroupList()
    {
        // Arrange
        var sut = BuildSut();
        var groups = new List<AccessGroup>
        {
            AccessGroup.Create(CompanyId, "admin-empresa", Permissions.All),
            AccessGroup.Create(CompanyId, "viewer", new[] { Permissions.FundsRead })
        };
        _accessGroupRepo.GetByCompanyIdAsync(CompanyId, Arg.Any<CancellationToken>())
            .Returns(groups);

        // Act
        var result = await sut.GetAccessGroups(CompanyId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var dtos = ok.Value.ShouldBeOfType<List<AccessGroupDto>>();
        dtos.Count.ShouldBe(2);
    }

    // =========================================================================
    // POST /api/companies/{companyId}/access-groups
    // =========================================================================

    [Fact]
    public async Task CreateAccessGroup_WhenCompanyIdMismatch_Returns403()
    {
        // Arrange
        var sut = BuildSut();

        // Act
        var result = await sut.CreateAccessGroup(Guid.NewGuid(), new CreateAccessGroupRequest("custom", [Permissions.FundsRead]), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CreateAccessGroup_WhenSuccess_Returns201()
    {
        // Arrange
        var sut = BuildSut();
        var dto = new AccessGroupDto(Guid.NewGuid(), "custom", new[] { Permissions.FundsRead });
        _createAccessGroupHandler
            .HandleAsync(Arg.Any<CreateAccessGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        // Act
        var result = await sut.CreateAccessGroup(CompanyId, new CreateAccessGroupRequest("custom", [Permissions.FundsRead]), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<CreatedAtActionResult>().StatusCode.ShouldBe(201);
    }

    [Fact]
    public async Task CreateAccessGroup_WhenBadRequestException_Returns400()
    {
        // Arrange
        var sut = BuildSut();
        _createAccessGroupHandler
            .HandleAsync(Arg.Any<CreateAccessGroupCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new BadRequestException("Invalid permissions."));

        // Act
        var result = await sut.CreateAccessGroup(CompanyId, new CreateAccessGroupRequest("bad", []), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>().StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task CreateAccessGroup_WhenKeyNotFoundException_Returns404()
    {
        // Arrange
        var sut = BuildSut();
        _createAccessGroupHandler
            .HandleAsync(Arg.Any<CreateAccessGroupCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Company not found."));

        // Act
        var result = await sut.CreateAccessGroup(CompanyId, new CreateAccessGroupRequest("group", [Permissions.FundsRead]), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateAccessGroup_PassesCorrectCommandFields()
    {
        // Arrange — exercises CreateAccessGroupRequest wiring
        var sut = BuildSut();
        var permissions = new[] { Permissions.FundsRead, Permissions.EmployeesRead };
        var dto = new AccessGroupDto(Guid.NewGuid(), "reader", permissions);
        _createAccessGroupHandler
            .HandleAsync(Arg.Any<CreateAccessGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        // Act
        await sut.CreateAccessGroup(CompanyId, new CreateAccessGroupRequest("reader", permissions), CancellationToken.None);

        // Assert
        await _createAccessGroupHandler.Received(1)
            .HandleAsync(Arg.Is<CreateAccessGroupCommand>(c =>
                c.CompanyId == CompanyId && c.Name == "reader"),
                Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // PUT /api/companies/{companyId}/access-groups/{id}
    // =========================================================================

    [Fact]
    public async Task UpdateAccessGroup_WhenCompanyIdMismatch_Returns403()
    {
        // Arrange
        var sut = BuildSut();

        // Act
        var result = await sut.UpdateAccessGroup(Guid.NewGuid(), Guid.NewGuid(), new UpdateAccessGroupRequest("new-name", null), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateAccessGroup_WhenSuccess_Returns200WithDto()
    {
        // Arrange
        var sut = BuildSut();
        var groupId = Guid.NewGuid();
        var dto = new AccessGroupDto(groupId, "updated", new[] { Permissions.FundsRead });
        _updateAccessGroupHandler
            .HandleAsync(Arg.Any<UpdateAccessGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        // Act
        var result = await sut.UpdateAccessGroup(CompanyId, groupId, new UpdateAccessGroupRequest("updated", new[] { Permissions.FundsRead }), CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        ok.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task UpdateAccessGroup_WhenBadRequestException_Returns400()
    {
        // Arrange
        var sut = BuildSut();
        _updateAccessGroupHandler
            .HandleAsync(Arg.Any<UpdateAccessGroupCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new BadRequestException("Cannot update default group."));

        // Act
        var result = await sut.UpdateAccessGroup(CompanyId, Guid.NewGuid(), new UpdateAccessGroupRequest(null, null), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>().StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task UpdateAccessGroup_WhenNotFound_Returns404()
    {
        // Arrange
        var sut = BuildSut();
        _updateAccessGroupHandler
            .HandleAsync(Arg.Any<UpdateAccessGroupCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Group not found."));

        // Act
        var result = await sut.UpdateAccessGroup(CompanyId, Guid.NewGuid(), new UpdateAccessGroupRequest("x", null), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateAccessGroup_PassesNullNameAndPermissionsToCommand()
    {
        // Arrange — exercises UpdateAccessGroupRequest(null, null) branch
        var sut = BuildSut();
        var dto = new AccessGroupDto(Guid.NewGuid(), "existing", new[] { Permissions.FundsRead });
        _updateAccessGroupHandler
            .HandleAsync(Arg.Any<UpdateAccessGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(dto);
        var groupId = Guid.NewGuid();

        // Act
        await sut.UpdateAccessGroup(CompanyId, groupId, new UpdateAccessGroupRequest(null, null), CancellationToken.None);

        // Assert
        await _updateAccessGroupHandler.Received(1)
            .HandleAsync(Arg.Is<UpdateAccessGroupCommand>(c =>
                c.AccessGroupId == groupId && c.Name == null && c.Permissions == null),
                Arg.Any<CancellationToken>());
    }

    // =========================================================================
    // DELETE /api/companies/{companyId}/access-groups/{id}
    // =========================================================================

    [Fact]
    public async Task DeleteAccessGroup_WhenCompanyIdMismatch_Returns403()
    {
        // Arrange
        var sut = BuildSut();

        // Act
        var result = await sut.DeleteAccessGroup(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteAccessGroup_WhenSuccess_Returns204()
    {
        // Arrange
        var sut = BuildSut();
        _deleteAccessGroupHandler
            .HandleAsync(Arg.Any<DeleteAccessGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Unit.Value);

        // Act
        var result = await sut.DeleteAccessGroup(CompanyId, Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NoContentResult>().StatusCode.ShouldBe(204);
    }

    [Fact]
    public async Task DeleteAccessGroup_WhenBadRequestException_Returns400()
    {
        // Arrange
        var sut = BuildSut();
        _deleteAccessGroupHandler
            .HandleAsync(Arg.Any<DeleteAccessGroupCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new BadRequestException("Cannot delete default group."));

        // Act
        var result = await sut.DeleteAccessGroup(CompanyId, Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>().StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task DeleteAccessGroup_WhenNotFound_Returns404()
    {
        // Arrange
        var sut = BuildSut();
        _deleteAccessGroupHandler
            .HandleAsync(Arg.Any<DeleteAccessGroupCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Group not found."));

        // Act
        var result = await sut.DeleteAccessGroup(CompanyId, Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();
    }

    // =========================================================================
    // Request DTO coverage — instantiation tests (ensures records are covered)
    // =========================================================================

    [Fact]
    public void RegisterEmployeeRequest_Properties_AreReadable()
    {
        // Arrange + Act
        var groupId = Guid.NewGuid();
        var r = new RegisterEmployeeRequest("João", "12345678909", "jo@t.com", "11999999999", groupId);

        // Assert
        r.Nome.ShouldBe("João");
        r.Cpf.ShouldBe("12345678909");
        r.Email.ShouldBe("jo@t.com");
        r.Phone.ShouldBe("11999999999");
        r.AccessGroupId.ShouldBe(groupId);
    }

    [Fact]
    public void ToggleStatusRequest_Properties_AreReadable()
    {
        var r = new ToggleStatusRequest(true);
        r.Activate.ShouldBeTrue();
    }

    [Fact]
    public void UpdateEmployeeRequest_Properties_AreReadable()
    {
        var r = new UpdateEmployeeRequest("Updated", "upd@t.com", "11988888888");
        r.Nome.ShouldBe("Updated");
        r.Email.ShouldBe("upd@t.com");
        r.Phone.ShouldBe("11988888888");
    }

    [Fact]
    public void ChangeAccessGroupRequest_Properties_AreReadable()
    {
        var groupId = Guid.NewGuid();
        var r = new ChangeAccessGroupRequest(groupId);
        r.AccessGroupId.ShouldBe(groupId);
    }

    [Fact]
    public void CreateAccessGroupRequest_Properties_AreReadable()
    {
        var perms = new[] { Permissions.FundsRead };
        var r = new CreateAccessGroupRequest("reader-group", perms);
        r.Name.ShouldBe("reader-group");
        r.Permissions.ShouldBe(perms);
    }

    [Fact]
    public void UpdateAccessGroupRequest_Properties_AreReadable()
    {
        var perms = new[] { Permissions.FundsWrite };
        var r = new UpdateAccessGroupRequest("writer-group", perms);
        r.Name.ShouldBe("writer-group");
        r.Permissions.ShouldBe(perms);
    }

    [Fact]
    public void UpdateAccessGroupRequest_WithNullProperties_IsValid()
    {
        var r = new UpdateAccessGroupRequest(null, null);
        r.Name.ShouldBeNull();
        r.Permissions.ShouldBeNull();
    }

    [Fact]
    public void CompanyProfileDto_Properties_AreReadable()
    {
        var id = Guid.NewGuid();
        var dto = new CompanyProfileDto(id, "ACME", "acme@test.com", "11999999999", "11222333000181");
        dto.Id.ShouldBe(id);
        dto.RazaoSocial.ShouldBe("ACME");
        dto.Email.ShouldBe("acme@test.com");
        dto.Phone.ShouldBe("11999999999");
        dto.Cnpj.ShouldBe("11222333000181");
    }
}
