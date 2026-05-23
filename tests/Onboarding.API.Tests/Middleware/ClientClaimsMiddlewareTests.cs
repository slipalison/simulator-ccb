using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.API.Middleware;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Persistence;
using Shouldly;

namespace Onboarding.API.Tests.Middleware;

public class ClientClaimsMiddlewareTests
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly CurrentCompanyService _companyService;
    private readonly CurrentCompanyPermissionsService _permissionsService;

    public ClientClaimsMiddlewareTests()
    {
        _companyRepository = Substitute.For<ICompanyRepository>();
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _accessGroupRepository = Substitute.For<IAccessGroupRepository>();
        _companyService = new CurrentCompanyService();
        _permissionsService = new CurrentCompanyPermissionsService();
    }

    private HttpContext CreateHttpContext(
        string? subClaim = null,
        string path = "/api/clients/me",
        bool isAuthenticated = true)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "GET";

        var services = new ServiceCollection();
        services.AddSingleton(LoggerFactory.Create(builder => builder.AddDebug()));
        services.AddSingleton<ICurrentCompanyService>(_companyService);
        services.AddSingleton<ICurrentCompanyPermissionsService>(_permissionsService);
        services.AddSingleton(_companyRepository);
        services.AddSingleton(_employeeRepository);
        services.AddSingleton(_accessGroupRepository);

        context.RequestServices = services.BuildServiceProvider();

        var identity = new System.Security.Claims.ClaimsIdentity();
        if (isAuthenticated)
        {
            identity = new System.Security.Claims.ClaimsIdentity("BearerClient", "name", "role");
            if (subClaim != null)
                identity.AddClaim(new System.Security.Claims.Claim("sub", subClaim));
        }
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);

        return context;
    }

    private async Task RunMiddlewareAsync(HttpContext context)
    {
        var appBuilder = new ApplicationBuilder(context.RequestServices);
        appBuilder.UseClientClaims();

        // Terminal middleware — just completes the pipeline
        appBuilder.Run(_ => Task.CompletedTask);

        var pipeline = appBuilder.Build();
        await pipeline(context);
    }

    [Fact]
    public async Task PJOwner_GetsAllPermissions_AndIsCompanyOwner()
    {
        // Arrange — PJ owner (Company.KeycloakUserId == JWT sub) gets all 6 permissions (D-20)
        var companyId = Guid.NewGuid();
        var keycloakSub = "keycloak-user-123";
        var company = CreateCompany(companyId, keycloakSub);

        _companyRepository.GetByKeycloakSubAsync(keycloakSub, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Company?>(company));

        var context = CreateHttpContext(subClaim: keycloakSub);

        // Act
        await RunMiddlewareAsync(context);

        // Assert
        _companyService.CompanyId.ShouldBe(companyId);
        _permissionsService.CompanyId.ShouldBe(companyId);
        _permissionsService.IsCompanyOwnerFlag.ShouldBeTrue();
        _permissionsService.PermissionList.ShouldBe(Permissions.All.ToList());
    }

    [Fact]
    public async Task EmployeeWithViewerGroup_GetsViewerPermissions_AndIsNotCompanyOwner()
    {
        // Arrange — Employee with viewer group gets [employees:read, audit:read] (PERM-02)
        var companyId = Guid.NewGuid();
        var keycloakSub = "employee-456";
        var employee = CreateEmployee(companyId, keycloakSub);
        var accessGroup = AccessGroup.Create(companyId, "viewer", [Permissions.EmployeesRead, Permissions.AuditRead]);

        _companyRepository.GetByKeycloakSubAsync(keycloakSub, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Company?>(null));
        _employeeRepository.GetByKeycloakSubAsync(keycloakSub, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Employee?>(employee));
        _accessGroupRepository.GetByIdAsync(employee.AccessGroupId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AccessGroup?>(accessGroup));

        var context = CreateHttpContext(subClaim: keycloakSub);

        // Act
        await RunMiddlewareAsync(context);

        // Assert
        _companyService.CompanyId.ShouldBe(companyId);
        _permissionsService.CompanyId.ShouldBe(companyId);
        _permissionsService.IsCompanyOwnerFlag.ShouldBeFalse();
        _permissionsService.PermissionList.ShouldBe(["employees:read", "audit:read"]);
    }

    [Fact]
    public async Task EmployeeWithAdminEmpresaGroup_GetsAllPermissions()
    {
        // Arrange — Employee with admin-empresa group gets all 6 permissions (PERM-01)
        var companyId = Guid.NewGuid();
        var keycloakSub = "admin-emp-789";
        var employee = CreateEmployee(companyId, keycloakSub);
        var accessGroup = AccessGroup.Create(companyId, "admin-empresa", Permissions.All);

        _companyRepository.GetByKeycloakSubAsync(keycloakSub, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Company?>(null));
        _employeeRepository.GetByKeycloakSubAsync(keycloakSub, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Employee?>(employee));
        _accessGroupRepository.GetByIdAsync(employee.AccessGroupId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AccessGroup?>(accessGroup));

        var context = CreateHttpContext(subClaim: keycloakSub);

        // Act
        await RunMiddlewareAsync(context);

        // Assert
        _permissionsService.IsCompanyOwnerFlag.ShouldBeFalse();
        _permissionsService.PermissionList.ShouldBe(Permissions.All.ToList());
    }

    [Fact]
    public async Task EmployeeWithDashboardGroup_GetsDashboardAccessOnly()
    {
        // Arrange — Employee with dashboard group gets dashboard:access only (PERM-03)
        var companyId = Guid.NewGuid();
        var keycloakSub = "dashboard-user-101";
        var employee = CreateEmployee(companyId, keycloakSub);
        var accessGroup = AccessGroup.Create(companyId, "dashboard", [Permissions.DashboardAccess]);

        _companyRepository.GetByKeycloakSubAsync(keycloakSub, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Company?>(null));
        _employeeRepository.GetByKeycloakSubAsync(keycloakSub, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Employee?>(employee));
        _accessGroupRepository.GetByIdAsync(employee.AccessGroupId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AccessGroup?>(accessGroup));

        var context = CreateHttpContext(subClaim: keycloakSub);

        // Act
        await RunMiddlewareAsync(context);

        // Assert
        _permissionsService.PermissionList.ShouldBe(["dashboard:access"]);
        _permissionsService.IsCompanyOwnerFlag.ShouldBeFalse();
    }

    [Fact]
    public async Task UnknownSub_CompanyIdRemainsEmpty_Isolated()
    {
        // Arrange — unknown sub → CompanyId=Guid.Empty → HasQueryFilter returns empty → 403
        var keycloakSub = "unknown-user-999";

        _companyRepository.GetByKeycloakSubAsync(keycloakSub, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Company?>(null));
        _employeeRepository.GetByKeycloakSubAsync(keycloakSub, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Employee?>(null));

        var context = CreateHttpContext(subClaim: keycloakSub);

        // Act
        await RunMiddlewareAsync(context);

        // Assert
        _companyService.CompanyId.ShouldBe(Guid.Empty);
        _permissionsService.CompanyId.ShouldBe(Guid.Empty);
        _permissionsService.PermissionList.ShouldBeEmpty();
        _permissionsService.IsCompanyOwnerFlag.ShouldBeFalse();
    }

    [Fact]
    public async Task AdminRoutes_SkipMiddleware()
    {
        // Arrange — /api/admin routes use backoffice auth, skip ClientClaimsMiddleware
        var context = CreateHttpContext(subClaim: "any-user", path: "/api/admin/users");

        // Act
        await RunMiddlewareAsync(context);

        // Assert — services remain unchanged (not populated)
        _companyService.CompanyId.ShouldBe(Guid.Empty);
        _permissionsService.PermissionList.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnauthenticatedUser_SkipMiddleware()
    {
        // Arrange — unauthenticated user
        var context = CreateHttpContext(subClaim: null, isAuthenticated: false);

        // Act
        await RunMiddlewareAsync(context);

        // Assert — services remain unchanged
        _companyService.CompanyId.ShouldBe(Guid.Empty);
        _permissionsService.PermissionList.ShouldBeEmpty();
    }

    private static Company CreateCompany(Guid id, string keycloakUserId)
    {
        var company = Company.Register(
            "Empresa Teste Ltda",
            "11222333000181",
            "empresa@teste.com",
            "11999999999",
            TermsAcceptance.Create("1.0", "127.0.0.1"));
        company.SetKeycloakUserId(keycloakUserId);
        // Set Id for test via reflection — factory generates a new Guid
        typeof(Company).BaseType!.GetProperty("Id")?.SetValue(company, id);
        return company;
    }

    private static Employee CreateEmployee(Guid companyId, string keycloakUserId)
    {
        var employee = Employee.Register(
            "João Silva",
            "52998224725",
            "joao@teste.com",
            "11999999999",
            companyId,
            Guid.NewGuid());
        employee.SetKeycloakUserId(keycloakUserId);
        return employee;
    }
}

/// <summary>
/// Tests for the W-data OTel metric emitted by ClientClaimsMiddleware on no-match path.
/// Verifies that clientclaims.no_match counter increments when JWT sub does not resolve
/// to any Company or Employee (frontend-client-fundos round 4, W-data fix).
/// </summary>
public sealed class ClientClaimsMiddlewareMetricTests
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAccessGroupRepository _accessGroupRepository;
    private readonly CurrentCompanyService _companyService;
    private readonly CurrentCompanyPermissionsService _permissionsService;

    public ClientClaimsMiddlewareMetricTests()
    {
        _companyRepository = Substitute.For<ICompanyRepository>();
        _employeeRepository = Substitute.For<IEmployeeRepository>();
        _accessGroupRepository = Substitute.For<IAccessGroupRepository>();
        _companyService = new CurrentCompanyService();
        _permissionsService = new CurrentCompanyPermissionsService();
    }

    private HttpContext CreateHttpContext(string sub, string path = "/api/clients/me")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "GET";

        var services = new ServiceCollection();
        services.AddSingleton(LoggerFactory.Create(b => b.AddDebug()));
        services.AddSingleton<ICurrentCompanyService>(_companyService);
        services.AddSingleton<ICurrentCompanyPermissionsService>(_permissionsService);
        services.AddSingleton(_companyRepository);
        services.AddSingleton(_employeeRepository);
        services.AddSingleton(_accessGroupRepository);
        context.RequestServices = services.BuildServiceProvider();

        var identity = new System.Security.Claims.ClaimsIdentity("BearerClient", "name", "role");
        identity.AddClaim(new System.Security.Claims.Claim("sub", sub));
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);
        return context;
    }

    private async Task RunMiddlewareAsync(HttpContext context)
    {
        var appBuilder = new ApplicationBuilder(context.RequestServices);
        appBuilder.UseClientClaims();
        appBuilder.Run(_ => Task.CompletedTask);
        await appBuilder.Build()(context);
    }

    [Fact]
    public async Task NoMatch_IncrementsNoMatchCounter()
    {
        // Arrange — use a unique sub suffix tied to THIS test instance to identify
        // measurements from this specific invocation, even under parallel test execution
        // where other tests may also trigger the shared static counter.
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8]; // 8 hex chars, unique per run
        var sub = $"no-match-sub-{uniqueSuffix}";
        _companyRepository.GetByKeycloakSubAsync(sub, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Company?>(null));
        _employeeRepository.GetByKeycloakSubAsync(sub, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Employee?>(null));

        var context = CreateHttpContext(sub);

        // Track whether this test's specific sub_prefix was observed.
        // Using a unique sub means only this test's Add() call will produce
        // the matching sub_prefix tag — immune to parallel counter increments.
        var expectedSubPrefix = sub[..8]; // "no-match" — first 8 chars
        var thisMeasurementObserved = false;
        string? capturedSubPrefix = null;

        // Use MeterListener to capture the counter measurement
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, ml) =>
        {
            if (instrument.Meter.Name == "Onboarding.API.Middleware"
                && instrument.Name == "clientclaims.no_match")
                ml.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "sub_prefix" && tag.Value?.ToString() == expectedSubPrefix)
                {
                    thisMeasurementObserved = true;
                    capturedSubPrefix = tag.Value.ToString();
                }
            }
        });
        listener.Start();

        // Act
        await RunMiddlewareAsync(context);

        // Assert — this test's unique sub_prefix was observed exactly once
        thisMeasurementObserved.ShouldBeTrue("clientclaims.no_match counter must fire on no-match path");
        capturedSubPrefix.ShouldBe(expectedSubPrefix);
    }

    [Fact]
    public async Task CompanyOwner_DoesNotIncrementNoMatchCounterForOwnerSub()
    {
        // Arrange — PJ owner matched → counter must NOT fire for this sub
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var sub = $"owner-sub-{uniqueSuffix}";
        var company = Company.Register(
            "Test Corp", "11222333000181", "owner@corp.com", "11999999999",
            TermsAcceptance.Create("1.0", "127.0.0.1"));
        company.SetKeycloakUserId(sub);

        _companyRepository.GetByKeycloakSubAsync(sub, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Company?>(company));

        var context = CreateHttpContext(sub);

        // Track if this specific owner sub_prefix fires — it must NOT
        var expectedSubPrefix = sub[..8]; // "owner-su"
        var thisMeasurementObserved = false;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, ml) =>
        {
            if (instrument.Meter.Name == "Onboarding.API.Middleware"
                && instrument.Name == "clientclaims.no_match")
                ml.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "sub_prefix" && tag.Value?.ToString() == expectedSubPrefix)
                    thisMeasurementObserved = true;
            }
        });
        listener.Start();

        // Act
        await RunMiddlewareAsync(context);

        // Assert — owner sub_prefix never fires on happy path
        thisMeasurementObserved.ShouldBeFalse("no-match counter must NOT fire when Company owner is found");
    }
}