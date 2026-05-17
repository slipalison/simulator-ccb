using System.Reflection;
using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Onboarding.API.Controllers;
using Onboarding.API.Security;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateCedenteTipoAtivo;
using Onboarding.Application.Fundos.Commands.TransitionCedenteTipoAtivoStatus;
using Onboarding.Application.Fundos.Commands.UpdateCedenteTipoAtivoLimite;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries.GetCedenteTiposAtivos;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.CedenteTipoAtivoAggregate;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.API.Tests.Controllers;

/// <summary>
/// Unit tests for CedenteTiposAtivosController — 5 endpoints.
/// Security: [Authorize(Policy = FundRead|FundWrite)] via reflection.
/// Tenant guard: cross-tenant Cedente → 404.
/// </summary>
public class CedenteTiposAtivosControllerTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid CedenteId = Guid.NewGuid();
    private static readonly Guid AssocId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedAt = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ICommandHandler<CreateCedenteTipoAtivoCommand, RelCedenteTipoAtivoDto> _createHandler =
        Substitute.For<ICommandHandler<CreateCedenteTipoAtivoCommand, RelCedenteTipoAtivoDto>>();
    private readonly ICommandHandler<UpdateCedenteTipoAtivoLimiteCommand, RelCedenteTipoAtivoDto> _updateHandler =
        Substitute.For<ICommandHandler<UpdateCedenteTipoAtivoLimiteCommand, RelCedenteTipoAtivoDto>>();
    private readonly ICommandHandler<TransitionCedenteTipoAtivoStatusCommand, RelCedenteTipoAtivoDto> _transitionHandler =
        Substitute.For<ICommandHandler<TransitionCedenteTipoAtivoStatusCommand, RelCedenteTipoAtivoDto>>();
    private readonly IQueryHandler<GetCedenteTiposAtivosQuery, PaginatedResult<RelCedenteTipoAtivoDto>> _listHandler =
        Substitute.For<IQueryHandler<GetCedenteTiposAtivosQuery, PaginatedResult<RelCedenteTipoAtivoDto>>>();
    private readonly ICedenteTipoAtivoAggregateRepository _repo =
        Substitute.For<ICedenteTipoAtivoAggregateRepository>();
    private readonly ICedenteRepository _cedenteRepo = Substitute.For<ICedenteRepository>();
    private readonly ICurrentCompanyService _company = Substitute.For<ICurrentCompanyService>();

    private readonly CedenteTiposAtivosController _sut;

    private static readonly RelCedenteTipoAtivoDto SampleDto =
        new(AssocId, CedenteId, Guid.NewGuid(), 30m, null, FixedAt, null, RelationshipStatus.ATIVO, FixedAt);

    public CedenteTiposAtivosControllerTests()
    {
        _company.CompanyId.Returns(CompanyId);

        _sut = new CedenteTiposAtivosController(
            _createHandler, _updateHandler, _transitionHandler, _listHandler,
            _repo, _cedenteRepo, _company);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim("sub", "test-sub"),
                    new Claim("email", "actor@test.com")
                }, "TestAuth"))
            }
        };
    }

    private static Cedente MakeCedente(Guid? clientId = null)
        => Cedente.RegisterPf("52998224725", "Cedente Teste", clientId ?? CompanyId);

    // =========================================================================
    // Security: [Authorize(Policy = ...)] via reflection
    // =========================================================================

    [Theory]
    [InlineData(nameof(CedenteTiposAtivosController.CreateCedenteTipoAtivo), PermissionPolicies.FundWrite)]
    [InlineData(nameof(CedenteTiposAtivosController.ListCedenteTiposAtivos), PermissionPolicies.FundRead)]
    [InlineData(nameof(CedenteTiposAtivosController.GetCedenteTipoAtivoById), PermissionPolicies.FundRead)]
    [InlineData(nameof(CedenteTiposAtivosController.UpdateCedenteTipoAtivoLimits), PermissionPolicies.FundWrite)]
    [InlineData(nameof(CedenteTiposAtivosController.TransitionCedenteTipoAtivoStatus), PermissionPolicies.FundWrite)]
    public void Endpoint_HasExpectedAuthorizePolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(CedenteTiposAtivosController).GetMethods()
            .Single(m => m.Name == methodName);
        var attrs = method.GetCustomAttributes<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().ToList();
        attrs.ShouldNotBeEmpty($"Method {methodName} missing [Authorize]");
        attrs.Select(a => a.Policy).ShouldContain(expectedPolicy);
    }

    [Fact]
    public void Controller_HasClassLevelBearerClientScheme()
    {
        var attr = typeof(CedenteTiposAtivosController)
            .GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        attr.ShouldNotBeNull();
        attr!.AuthenticationSchemes.ShouldBe("BearerClient");
    }

    // =========================================================================
    // CreateCedenteTipoAtivo
    // =========================================================================

    [Fact]
    public async Task CreateCedenteTipoAtivo_NullBody_Returns400()
    {
        var result = await _sut.CreateCedenteTipoAtivo(CedenteId, null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateCedenteTipoAtivo_CrossTenantCedente_Returns404()
    {
        _cedenteRepo.GetByIdAsync(CedenteId, Arg.Any<CancellationToken>())
            .Returns(MakeCedente(Guid.NewGuid())); // different company

        var request = new CreateCedenteTipoAtivoRequest(Guid.NewGuid(), 30m, null, DateTimeOffset.UtcNow, null);
        var result = await _sut.CreateCedenteTipoAtivo(CedenteId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateCedenteTipoAtivo_CedenteNotFound_Returns404()
    {
        _cedenteRepo.GetByIdAsync(CedenteId, Arg.Any<CancellationToken>()).Returns((Cedente?)null);

        var request = new CreateCedenteTipoAtivoRequest(Guid.NewGuid(), 30m, null, DateTimeOffset.UtcNow, null);
        var result = await _sut.CreateCedenteTipoAtivo(CedenteId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateCedenteTipoAtivo_DuplicateActive_Returns409()
    {
        _cedenteRepo.GetByIdAsync(CedenteId, Arg.Any<CancellationToken>()).Returns(MakeCedente());
        _createHandler
            .HandleAsync(Arg.Any<CreateCedenteTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateActiveAssociationException("CedenteTipoAtivo", "pair already active."));

        var request = new CreateCedenteTipoAtivoRequest(Guid.NewGuid(), 30m, null, DateTimeOffset.UtcNow, null);
        var result = await _sut.CreateCedenteTipoAtivo(CedenteId, request, CancellationToken.None);

        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateCedenteTipoAtivo_ValidationException_Returns422()
    {
        _cedenteRepo.GetByIdAsync(CedenteId, Arg.Any<CancellationToken>()).Returns(MakeCedente());
        _createHandler
            .HandleAsync(Arg.Any<CreateCedenteTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ValidationException(new[] { new ValidationFailure("LimitePercentual", "Required.") }));

        var request = new CreateCedenteTipoAtivoRequest(Guid.NewGuid(), null, null, DateTimeOffset.UtcNow, null);
        var result = await _sut.CreateCedenteTipoAtivo(CedenteId, request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task CreateCedenteTipoAtivo_HappyPath_Returns201()
    {
        _cedenteRepo.GetByIdAsync(CedenteId, Arg.Any<CancellationToken>()).Returns(MakeCedente());
        _createHandler
            .HandleAsync(Arg.Any<CreateCedenteTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleDto);

        var request = new CreateCedenteTipoAtivoRequest(Guid.NewGuid(), 30m, null, DateTimeOffset.UtcNow, null);
        var result = await _sut.CreateCedenteTipoAtivo(CedenteId, request, CancellationToken.None);

        var created = result.ShouldBeOfType<CreatedAtActionResult>();
        created.Value.ShouldBe(SampleDto);
        created.ActionName.ShouldBe(nameof(CedenteTiposAtivosController.GetCedenteTipoAtivoById));
    }

    // =========================================================================
    // ListCedenteTiposAtivos
    // =========================================================================

    [Fact]
    public async Task ListCedenteTiposAtivos_CrossTenantCedente_Returns404()
    {
        _cedenteRepo.GetByIdAsync(CedenteId, Arg.Any<CancellationToken>())
            .Returns(MakeCedente(Guid.NewGuid()));

        var result = await _sut.ListCedenteTiposAtivos(CedenteId, ct: CancellationToken.None);
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ListCedenteTiposAtivos_HappyPath_Returns200()
    {
        _cedenteRepo.GetByIdAsync(CedenteId, Arg.Any<CancellationToken>()).Returns(MakeCedente());
        var paged = new PaginatedResult<RelCedenteTipoAtivoDto>(new[] { SampleDto }, 1, 1, 20);
        _listHandler
            .HandleAsync(Arg.Any<GetCedenteTiposAtivosQuery>(), Arg.Any<CancellationToken>())
            .Returns(paged);

        var result = await _sut.ListCedenteTiposAtivos(CedenteId, ct: CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(paged);
    }

    // =========================================================================
    // GetCedenteTipoAtivoById
    // =========================================================================

    [Fact]
    public async Task GetCedenteTipoAtivoById_CrossTenantCedente_Returns404()
    {
        _cedenteRepo.GetByIdAsync(CedenteId, Arg.Any<CancellationToken>())
            .Returns(MakeCedente(Guid.NewGuid()));

        var result = await _sut.GetCedenteTipoAtivoById(CedenteId, AssocId, CancellationToken.None);
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetCedenteTipoAtivoById_WrongCedente_Returns404()
    {
        _cedenteRepo.GetByIdAsync(CedenteId, Arg.Any<CancellationToken>()).Returns(MakeCedente());
        var assoc = CedenteTipoAtivoAggregate.Create(Guid.NewGuid(), Guid.NewGuid(),
            LimiteExposicao.Create(30m, null), JanelaVigencia.Create(DateTimeOffset.UtcNow));
        _repo.GetByIdAsync(AssocId, Arg.Any<CancellationToken>()).Returns(assoc);

        var result = await _sut.GetCedenteTipoAtivoById(CedenteId, AssocId, CancellationToken.None);
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetCedenteTipoAtivoById_HappyPath_Returns200()
    {
        _cedenteRepo.GetByIdAsync(CedenteId, Arg.Any<CancellationToken>()).Returns(MakeCedente());
        var assoc = CedenteTipoAtivoAggregate.Create(CedenteId, Guid.NewGuid(),
            LimiteExposicao.Create(30m, null), JanelaVigencia.Create(DateTimeOffset.UtcNow));
        _repo.GetByIdAsync(AssocId, Arg.Any<CancellationToken>()).Returns(assoc);

        var result = await _sut.GetCedenteTipoAtivoById(CedenteId, AssocId, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var dto = ok.Value.ShouldBeOfType<RelCedenteTipoAtivoDto>();
        dto.CedenteId.ShouldBe(CedenteId);
    }

    // =========================================================================
    // UpdateCedenteTipoAtivoLimits
    // =========================================================================

    [Fact]
    public async Task UpdateCedenteTipoAtivoLimits_NullBody_Returns400()
    {
        var result = await _sut.UpdateCedenteTipoAtivoLimits(CedenteId, AssocId, null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateCedenteTipoAtivoLimits_HappyPath_Returns200()
    {
        _cedenteRepo.GetByIdAsync(CedenteId, Arg.Any<CancellationToken>()).Returns(MakeCedente());
        _updateHandler
            .HandleAsync(Arg.Any<UpdateCedenteTipoAtivoLimiteCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleDto);

        var request = new UpdateRelationshipLimitsRequest(30m, null);
        var result = await _sut.UpdateCedenteTipoAtivoLimits(CedenteId, AssocId, request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(SampleDto);
    }

    // =========================================================================
    // TransitionCedenteTipoAtivoStatus
    // =========================================================================

    [Fact]
    public async Task TransitionCedenteTipoAtivoStatus_NullBody_Returns400()
    {
        var result = await _sut.TransitionCedenteTipoAtivoStatus(CedenteId, AssocId, null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task TransitionCedenteTipoAtivoStatus_CrossTenantCedente_Returns404()
    {
        _cedenteRepo.GetByIdAsync(CedenteId, Arg.Any<CancellationToken>())
            .Returns(MakeCedente(Guid.NewGuid()));

        var request = new TransitionRelationshipStatusRequest(RelationshipStatus.INATIVO);
        var result = await _sut.TransitionCedenteTipoAtivoStatus(CedenteId, AssocId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task TransitionCedenteTipoAtivoStatus_HappyPath_Returns200()
    {
        _cedenteRepo.GetByIdAsync(CedenteId, Arg.Any<CancellationToken>()).Returns(MakeCedente());
        _transitionHandler
            .HandleAsync(Arg.Any<TransitionCedenteTipoAtivoStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleDto with { Status = RelationshipStatus.INATIVO });

        var request = new TransitionRelationshipStatusRequest(RelationshipStatus.INATIVO);
        var result = await _sut.TransitionCedenteTipoAtivoStatus(CedenteId, AssocId, request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var dto = ok.Value.ShouldBeOfType<RelCedenteTipoAtivoDto>();
        dto.Status.ShouldBe(RelationshipStatus.INATIVO);
    }
}
