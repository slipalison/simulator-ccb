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

namespace Onboarding.API.Tests.Controllers;

/// <summary>
/// Controller-level tests for the audit-log endpoint entityType+entityId query params (Phase 52, T-1).
/// Verifies that new query params wire through to GetAuditLogQuery correctly.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AdminUserControllerAuditLogTests
{
    private readonly IQueryHandler<GetAuditLogQuery, PaginatedResult<AdminAuditLogDto>> _auditLogHandler;
    private readonly AdminUserController _sut;

    public AdminUserControllerAuditLogTests()
    {
        _auditLogHandler = Substitute.For<IQueryHandler<GetAuditLogQuery, PaginatedResult<AdminAuditLogDto>>>();

        _auditLogHandler
            .HandleAsync(Arg.Any<GetAuditLogQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminAuditLogDto>([], 0, 1, 20));

        _sut = new AdminUserController(
            Substitute.For<IQueryHandler<GetPaginatedCompaniesQuery, PaginatedResult<CompanySummaryDto>>>(),
            Substitute.For<IQueryHandler<GetCompanyDetailsQuery, CompanySummaryDto>>(),
            Substitute.For<IQueryHandler<GetPaginatedEmployeesQuery, PaginatedResult<EmployeeSummaryDto>>>(),
            Substitute.For<IQueryHandler<GetEmployeeDetailsQuery, EmployeeSummaryDto>>(),
            Substitute.For<ICommandHandler<UpdateCompanyCommand, Unit>>(),
            Substitute.For<ICommandHandler<DeleteEmployeeCommand, Unit>>(),
            Substitute.For<ICommandHandler<BlockEmployeeCommand, Unit>>(),
            Substitute.For<ICommandHandler<UnblockEmployeeCommand, Unit>>(),
            Substitute.For<ICommandHandler<CreateAdminCommand, CreateAdminResult>>(),
            Substitute.For<ICommandHandler<ForcePasswordChangeCommand, Unit>>(),
            _auditLogHandler,
            Substitute.For<IQueryHandler<GetAdministratorsQuery, IReadOnlyList<AdminUserDto>>>(),
            Substitute.For<IQueryHandler<GetPaginatedAdministratorsQuery, PaginatedResult<AdminUserDto>>>(),
            Substitute.For<ICommandHandler<UpdateAdministratorCommand, Unit>>(),
            Substitute.For<ICommandHandler<ResetAdministratorPasswordCommand, ResetAdministratorPasswordResult>>(),
            Substitute.For<ICommandHandler<ToggleAdministratorStatusCommand, Unit>>(),
            Substitute.For<IKeycloakUserService>(),
            Substitute.For<IValidator<CreateAdminCommand>>(),
            Substitute.For<IValidator<ForcePasswordChangeCommand>>(),
            Substitute.For<IValidator<UpdateAdministratorCommand>>(),
            Substitute.For<IValidator<ResetAdministratorPasswordCommand>>(),
            Substitute.For<IValidator<ToggleAdministratorStatusCommand>>(),
            Substitute.For<ILogger<AdminUserController>>()
        );

        // Wire authenticated user
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "admin-sub-001"),
            new Claim("email", "admin@backoffice.com"),
        }, "TestAuth"));
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user },
        };
    }

    [Fact]
    public async Task GetAuditLog_WithNoEntityParams_PassesNullEntityTypeAndId()
    {
        await _sut.GetAuditLog(page: 1, pageSize: 20);

        await _auditLogHandler.Received(1).HandleAsync(
            Arg.Is<GetAuditLogQuery>(q => q.EntityType == null && q.EntityId == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAuditLog_WithEntityTypeParam_PassesEntityTypeToQuery()
    {
        await _sut.GetAuditLog(entityType: "Fundo");

        await _auditLogHandler.Received(1).HandleAsync(
            Arg.Is<GetAuditLogQuery>(q => q.EntityType == "Fundo" && q.EntityId == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAuditLog_WithEntityTypeAndEntityId_PassesBothToQuery()
    {
        var entityId = Guid.NewGuid();

        await _sut.GetAuditLog(entityType: "Fundo", entityId: entityId);

        await _auditLogHandler.Received(1).HandleAsync(
            Arg.Is<GetAuditLogQuery>(q => q.EntityType == "Fundo" && q.EntityId == entityId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAuditLog_BackwardCompat_NullParams_Returns200()
    {
        var result = await _sut.GetAuditLog();

        result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAuditLog_WithEntityType_Returns200()
    {
        var result = await _sut.GetAuditLog(entityType: "FundoCedente");

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeOfType<PaginatedResult<AdminAuditLogDto>>();
    }

    [Fact]
    public async Task GetAuditLog_WithAllFilters_PassesAllParamsToQuery()
    {
        var startDate = DateTimeOffset.UtcNow.AddDays(-7);
        var endDate = DateTimeOffset.UtcNow;
        var entityId = Guid.NewGuid();

        await _sut.GetAuditLog(
            page: 2,
            pageSize: 10,
            startDate: startDate,
            endDate: endDate,
            entityType: "FundoTipoAtivo",
            entityId: entityId);

        await _auditLogHandler.Received(1).HandleAsync(
            Arg.Is<GetAuditLogQuery>(q =>
                q.Page == 2 &&
                q.PageSize == 10 &&
                q.StartDate == startDate &&
                q.EndDate == endDate &&
                q.EntityType == "FundoTipoAtivo" &&
                q.EntityId == entityId),
            Arg.Any<CancellationToken>());
    }
}
