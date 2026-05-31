using System.Security.Claims;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.API.Controllers;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Admin.Queries;
using Onboarding.Application.Common;
using Shouldly;

namespace Onboarding.API.Tests.Controllers;

/// <summary>
/// Controller-level tests for the audit-log endpoint entityType+entityId query params (Phase 52, T-1).
/// Refactored for Phase 55 (D-60..D-63): uses IQueryDispatcher.
/// </summary>
[Trait("Category", "Unit")]
public sealed class AdminUserControllerAuditLogTests
{
    private readonly IQueryDispatcher _queries = Substitute.For<IQueryDispatcher>();
    private readonly AdminUserController _sut;

    public AdminUserControllerAuditLogTests()
    {
        _queries.Query<PaginatedResult<AdminAuditLogDto>>(Arg.Any<GetAuditLogQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminAuditLogDto>([], 0, 1, 20));

        _sut = new AdminUserController(
            Substitute.For<ICommandDispatcher>(),
            _queries,
            Substitute.For<IValidationRunner>(),
            Substitute.For<IKeycloakUserService>(),
            Substitute.For<ILogger<AdminUserController>>());

        // Default: validation passes
        var validation = Substitute.For<IValidationRunner>();
        validation.Validate(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

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
        GetAuditLogQuery? captured = null;
        _queries.Query<PaginatedResult<AdminAuditLogDto>>(
            Arg.Do<object>(q => captured = q as GetAuditLogQuery),
            Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminAuditLogDto>([], 0, 1, 20));

        await _sut.GetAuditLog(page: 1, pageSize: 20);

        captured.ShouldNotBeNull();
        captured!.EntityType.ShouldBeNull();
        captured.EntityId.ShouldBeNull();
    }

    [Fact]
    public async Task GetAuditLog_WithEntityTypeParam_PassesEntityTypeToQuery()
    {
        GetAuditLogQuery? captured = null;
        _queries.Query<PaginatedResult<AdminAuditLogDto>>(
            Arg.Do<object>(q => captured = q as GetAuditLogQuery),
            Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminAuditLogDto>([], 0, 1, 20));

        await _sut.GetAuditLog(entityType: "Fundo");

        captured.ShouldNotBeNull();
        captured!.EntityType.ShouldBe("Fundo");
        captured.EntityId.ShouldBeNull();
    }

    [Fact]
    public async Task GetAuditLog_WithEntityTypeAndEntityId_PassesBothToQuery()
    {
        var entityId = Guid.NewGuid();
        GetAuditLogQuery? captured = null;
        _queries.Query<PaginatedResult<AdminAuditLogDto>>(
            Arg.Do<object>(q => captured = q as GetAuditLogQuery),
            Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminAuditLogDto>([], 0, 1, 20));

        await _sut.GetAuditLog(entityType: "Fundo", entityId: entityId);

        captured.ShouldNotBeNull();
        captured!.EntityType.ShouldBe("Fundo");
        captured.EntityId.ShouldBe(entityId);
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

        GetAuditLogQuery? captured = null;
        _queries.Query<PaginatedResult<AdminAuditLogDto>>(
            Arg.Do<object>(q => captured = q as GetAuditLogQuery),
            Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<AdminAuditLogDto>([], 0, 1, 20));

        await _sut.GetAuditLog(
            page: 2,
            pageSize: 10,
            startDate: startDate,
            endDate: endDate,
            entityType: "FundoTipoAtivo",
            entityId: entityId);

        captured.ShouldNotBeNull();
        captured!.Page.ShouldBe(2);
        captured.PageSize.ShouldBe(10);
        captured.StartDate.ShouldBe(startDate);
        captured.EndDate.ShouldBe(endDate);
        captured.EntityType.ShouldBe("FundoTipoAtivo");
        captured.EntityId.ShouldBe(entityId);
    }
}
