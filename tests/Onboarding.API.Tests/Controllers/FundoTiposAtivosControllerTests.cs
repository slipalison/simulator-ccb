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
using Onboarding.Application.Fundos.Commands.CreateFundoTipoAtivo;
using Onboarding.Application.Fundos.Commands.TransitionFundoTipoAtivoStatus;
using Onboarding.Application.Fundos.Commands.UpdateFundoTipoAtivoLimite;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries.GetFundoTiposAtivos;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Aggregates.FundoTipoAtivoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.API.Tests.Controllers;

/// <summary>
/// Unit tests for FundoTiposAtivosController — 5 endpoints.
/// Security: [Authorize(Policy = FundRead|FundWrite)] via reflection.
/// Tenant guard: cross-tenant Fundo → 404.
/// </summary>
public class FundoTiposAtivosControllerTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid FundoId = Guid.NewGuid();
    private static readonly Guid AssocId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedAt = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ICommandHandler<CreateFundoTipoAtivoCommand, RelFundoTipoAtivoDto> _createHandler =
        Substitute.For<ICommandHandler<CreateFundoTipoAtivoCommand, RelFundoTipoAtivoDto>>();
    private readonly ICommandHandler<UpdateFundoTipoAtivoLimiteCommand, RelFundoTipoAtivoDto> _updateHandler =
        Substitute.For<ICommandHandler<UpdateFundoTipoAtivoLimiteCommand, RelFundoTipoAtivoDto>>();
    private readonly ICommandHandler<TransitionFundoTipoAtivoStatusCommand, RelFundoTipoAtivoDto> _transitionHandler =
        Substitute.For<ICommandHandler<TransitionFundoTipoAtivoStatusCommand, RelFundoTipoAtivoDto>>();
    private readonly IQueryHandler<GetFundoTiposAtivosQuery, PaginatedResult<RelFundoTipoAtivoDto>> _listHandler =
        Substitute.For<IQueryHandler<GetFundoTiposAtivosQuery, PaginatedResult<RelFundoTipoAtivoDto>>>();
    private readonly IFundoTipoAtivoAggregateRepository _repo =
        Substitute.For<IFundoTipoAtivoAggregateRepository>();
    private readonly IFundoRepository _fundoRepo = Substitute.For<IFundoRepository>();
    private readonly ICurrentCompanyService _company = Substitute.For<ICurrentCompanyService>();

    private readonly FundoTiposAtivosController _sut;

    private static readonly RelFundoTipoAtivoDto SampleDto =
        new(AssocId, FundoId, Guid.NewGuid(), 30m, null, FixedAt, null, RelationshipStatus.ATIVO, FixedAt);

    public FundoTiposAtivosControllerTests()
    {
        _company.CompanyId.Returns(CompanyId);

        _sut = new FundoTiposAtivosController(
            _createHandler, _updateHandler, _transitionHandler, _listHandler,
            _repo, _fundoRepo, _company);

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

    private Fundo MakeFundo(Guid? clientId = null)
        => Fundo.Register("Fundo", "11222333000181", clientId ?? CompanyId,
            Guid.NewGuid(), Guid.NewGuid(), TipoFundo.RendaFixa);

    // =========================================================================
    // Security: [Authorize(Policy = ...)] via reflection
    // =========================================================================

    [Theory]
    [InlineData(nameof(FundoTiposAtivosController.CreateFundoTipoAtivo), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundoTiposAtivosController.ListFundoTiposAtivos), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundoTiposAtivosController.GetFundoTipoAtivoById), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundoTiposAtivosController.UpdateFundoTipoAtivoLimits), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundoTiposAtivosController.TransitionFundoTipoAtivoStatus), PermissionPolicies.FundWrite)]
    public void Endpoint_HasExpectedAuthorizePolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(FundoTiposAtivosController).GetMethods()
            .Single(m => m.Name == methodName);
        var attrs = method.GetCustomAttributes<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().ToList();
        attrs.ShouldNotBeEmpty($"Method {methodName} missing [Authorize]");
        attrs.Select(a => a.Policy).ShouldContain(expectedPolicy);
    }

    [Fact]
    public void Controller_HasClassLevelBearerClientScheme()
    {
        var attr = typeof(FundoTiposAtivosController)
            .GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        attr.ShouldNotBeNull();
        attr!.AuthenticationSchemes.ShouldBe("BearerClient");
    }

    // =========================================================================
    // CreateFundoTipoAtivo
    // =========================================================================

    [Fact]
    public async Task CreateFundoTipoAtivo_NullBody_Returns400()
    {
        var result = await _sut.CreateFundoTipoAtivo(FundoId, null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateFundoTipoAtivo_CrossTenantFundo_Returns404()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>())
            .Returns(MakeFundo(Guid.NewGuid()));

        var request = new CreateFundoTipoAtivoRequest(Guid.NewGuid(), 30m, null, DateTimeOffset.UtcNow, null);
        var result = await _sut.CreateFundoTipoAtivo(FundoId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateFundoTipoAtivo_DuplicateActive_Returns409()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        _createHandler
            .HandleAsync(Arg.Any<CreateFundoTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateActiveAssociationException("FundoTipoAtivo", "pair already active."));

        var request = new CreateFundoTipoAtivoRequest(Guid.NewGuid(), 30m, null, DateTimeOffset.UtcNow, null);
        var result = await _sut.CreateFundoTipoAtivo(FundoId, request, CancellationToken.None);

        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateFundoTipoAtivo_HappyPath_Returns201()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        _createHandler
            .HandleAsync(Arg.Any<CreateFundoTipoAtivoCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleDto);

        var request = new CreateFundoTipoAtivoRequest(Guid.NewGuid(), 30m, null, DateTimeOffset.UtcNow, null);
        var result = await _sut.CreateFundoTipoAtivo(FundoId, request, CancellationToken.None);

        var created = result.ShouldBeOfType<CreatedAtActionResult>();
        created.Value.ShouldBe(SampleDto);
        created.ActionName.ShouldBe(nameof(FundoTiposAtivosController.GetFundoTipoAtivoById));
    }

    // =========================================================================
    // ListFundoTiposAtivos
    // =========================================================================

    [Fact]
    public async Task ListFundoTiposAtivos_CrossTenantFundo_Returns404()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>())
            .Returns(MakeFundo(Guid.NewGuid()));

        var result = await _sut.ListFundoTiposAtivos(FundoId, ct: CancellationToken.None);
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ListFundoTiposAtivos_HappyPath_Returns200()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        var paged = new PaginatedResult<RelFundoTipoAtivoDto>(new[] { SampleDto }, 1, 1, 20);
        _listHandler
            .HandleAsync(Arg.Any<GetFundoTiposAtivosQuery>(), Arg.Any<CancellationToken>())
            .Returns(paged);

        var result = await _sut.ListFundoTiposAtivos(FundoId, ct: CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(paged);
    }

    // =========================================================================
    // GetFundoTipoAtivoById
    // =========================================================================

    [Fact]
    public async Task GetFundoTipoAtivoById_CrossTenantFundo_Returns404()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>())
            .Returns(MakeFundo(Guid.NewGuid()));

        var result = await _sut.GetFundoTipoAtivoById(FundoId, AssocId, CancellationToken.None);
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetFundoTipoAtivoById_WrongFundo_Returns404()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        var assoc = FundoTipoAtivoAggregate.Create(Guid.NewGuid(), Guid.NewGuid(),
            LimiteExposicao.Create(30m, null), JanelaVigencia.Create(DateTimeOffset.UtcNow));
        _repo.GetByIdAsync(AssocId, Arg.Any<CancellationToken>()).Returns(assoc);

        var result = await _sut.GetFundoTipoAtivoById(FundoId, AssocId, CancellationToken.None);
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetFundoTipoAtivoById_HappyPath_Returns200()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        var assoc = FundoTipoAtivoAggregate.Create(FundoId, Guid.NewGuid(),
            LimiteExposicao.Create(30m, null), JanelaVigencia.Create(DateTimeOffset.UtcNow));
        _repo.GetByIdAsync(AssocId, Arg.Any<CancellationToken>()).Returns(assoc);

        var result = await _sut.GetFundoTipoAtivoById(FundoId, AssocId, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var dto = ok.Value.ShouldBeOfType<RelFundoTipoAtivoDto>();
        dto.FundoId.ShouldBe(FundoId);
    }

    // =========================================================================
    // UpdateFundoTipoAtivoLimits
    // =========================================================================

    [Fact]
    public async Task UpdateFundoTipoAtivoLimits_NullBody_Returns400()
    {
        var result = await _sut.UpdateFundoTipoAtivoLimits(FundoId, AssocId, null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateFundoTipoAtivoLimits_InvalidStateTransition_Returns400()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        _updateHandler
            .HandleAsync(Arg.Any<UpdateFundoTipoAtivoLimiteCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidStateTransitionException("Cannot update HISTORICO."));

        var request = new UpdateRelationshipLimitsRequest(30m, null);
        var result = await _sut.UpdateFundoTipoAtivoLimits(FundoId, AssocId, request, CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateFundoTipoAtivoLimits_HappyPath_Returns200()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        _updateHandler
            .HandleAsync(Arg.Any<UpdateFundoTipoAtivoLimiteCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleDto);

        var request = new UpdateRelationshipLimitsRequest(30m, null);
        var result = await _sut.UpdateFundoTipoAtivoLimits(FundoId, AssocId, request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(SampleDto);
    }

    // =========================================================================
    // TransitionFundoTipoAtivoStatus
    // =========================================================================

    [Fact]
    public async Task TransitionFundoTipoAtivoStatus_NullBody_Returns400()
    {
        var result = await _sut.TransitionFundoTipoAtivoStatus(FundoId, AssocId, null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task TransitionFundoTipoAtivoStatus_CrossTenantFundo_Returns404()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>())
            .Returns(MakeFundo(Guid.NewGuid()));

        var request = new TransitionRelationshipStatusRequest(RelationshipStatus.INATIVO);
        var result = await _sut.TransitionFundoTipoAtivoStatus(FundoId, AssocId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task TransitionFundoTipoAtivoStatus_HappyPath_Returns200()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        _transitionHandler
            .HandleAsync(Arg.Any<TransitionFundoTipoAtivoStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleDto with { Status = RelationshipStatus.INATIVO });

        var request = new TransitionRelationshipStatusRequest(RelationshipStatus.INATIVO);
        var result = await _sut.TransitionFundoTipoAtivoStatus(FundoId, AssocId, request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var dto = ok.Value.ShouldBeOfType<RelFundoTipoAtivoDto>();
        dto.Status.ShouldBe(RelationshipStatus.INATIVO);
    }
}
