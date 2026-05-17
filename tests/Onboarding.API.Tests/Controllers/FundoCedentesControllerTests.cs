using System.Reflection;
using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Onboarding.API.Controllers;
using Onboarding.API.Security;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands.CreateFundoCedente;
using Onboarding.Application.Fundos.Commands.TransitionFundoCedenteStatus;
using Onboarding.Application.Fundos.Commands.UpdateFundoCedenteLimite;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries.GetFundoCedentes;
using Onboarding.Application.Fundos.Queries.GetFundoCedenteAllowedTransitions;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Shouldly;

namespace Onboarding.API.Tests.Controllers;

/// <summary>
/// Unit tests for FundoCedentesController — 5 endpoints.
/// Security: [Authorize(Policy = FundRead|FundWrite)] via reflection.
/// Tenant guard: cross-tenant Fundo → 404.
/// Error paths: null body → 400, ValidationException → 422, Duplicate → 409, InvalidState → 400.
/// </summary>
public class FundoCedentesControllerTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid FundoId = Guid.NewGuid();
    private static readonly Guid AssocId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedAt = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ICommandHandler<CreateFundoCedenteCommand, RelFundoCedenteDto> _createHandler =
        Substitute.For<ICommandHandler<CreateFundoCedenteCommand, RelFundoCedenteDto>>();
    private readonly ICommandHandler<UpdateFundoCedenteLimiteCommand, RelFundoCedenteDto> _updateHandler =
        Substitute.For<ICommandHandler<UpdateFundoCedenteLimiteCommand, RelFundoCedenteDto>>();
    private readonly ICommandHandler<TransitionFundoCedenteStatusCommand, RelFundoCedenteDto> _transitionHandler =
        Substitute.For<ICommandHandler<TransitionFundoCedenteStatusCommand, RelFundoCedenteDto>>();
    private readonly IQueryHandler<GetFundoCedentesQuery, PaginatedResult<RelFundoCedenteDto>> _listHandler =
        Substitute.For<IQueryHandler<GetFundoCedentesQuery, PaginatedResult<RelFundoCedenteDto>>>();
    private readonly IFundoCedenteAggregateRepository _repo =
        Substitute.For<IFundoCedenteAggregateRepository>();
    private readonly IFundoRepository _fundoRepo = Substitute.For<IFundoRepository>();
    private readonly ICurrentCompanyService _company = Substitute.For<ICurrentCompanyService>();

    private readonly FundoCedentesController _sut;

    private static readonly RelFundoCedenteDto SampleDto =
        new(AssocId, FundoId, Guid.NewGuid(), 50m, null, FixedAt, null, RelationshipStatus.ATIVO, FixedAt);

    public FundoCedentesControllerTests()
    {
        _company.CompanyId.Returns(CompanyId);

        _sut = new FundoCedentesController(
            _createHandler, _updateHandler, _transitionHandler, _listHandler,
            Substitute.For<IQueryHandler<GetFundoCedenteAllowedTransitionsQuery, IReadOnlyList<string>?>>(),
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

    private FundoCedenteAggregate MakeAssoc()
        => FundoCedenteAggregate.Create(FundoId, Guid.NewGuid(),
            LimiteExposicao.Create(50m, null), JanelaVigencia.Create(DateTimeOffset.UtcNow));

    // =========================================================================
    // Security: [Authorize(Policy = ...)] via reflection
    // =========================================================================

    [Theory]
    [InlineData(nameof(FundoCedentesController.CreateFundoCedente), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundoCedentesController.ListFundoCedentes), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundoCedentesController.GetFundoCedenteById), PermissionPolicies.FundRead)]
    [InlineData(nameof(FundoCedentesController.UpdateFundoCedenteLimits), PermissionPolicies.FundWrite)]
    [InlineData(nameof(FundoCedentesController.TransitionFundoCedenteStatus), PermissionPolicies.FundWrite)]
    public void Endpoint_HasExpectedAuthorizePolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(FundoCedentesController).GetMethods()
            .Single(m => m.Name == methodName);
        var attrs = method.GetCustomAttributes<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().ToList();
        attrs.ShouldNotBeEmpty($"Method {methodName} missing [Authorize]");
        attrs.Select(a => a.Policy).ShouldContain(expectedPolicy);
    }

    [Fact]
    public void Controller_HasClassLevelBearerClientScheme()
    {
        var attr = typeof(FundoCedentesController)
            .GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        attr.ShouldNotBeNull();
        attr!.AuthenticationSchemes.ShouldBe("BearerClient");
    }

    // =========================================================================
    // CreateFundoCedente
    // =========================================================================

    [Fact]
    public async Task CreateFundoCedente_NullBody_Returns400()
    {
        var result = await _sut.CreateFundoCedente(FundoId, null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateFundoCedente_CrossTenantFundo_Returns404()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>())
            .Returns(MakeFundo(Guid.NewGuid())); // different company

        var request = new CreateFundoCedenteRequest(Guid.NewGuid(), 50m, null, DateTimeOffset.UtcNow, null);
        var result = await _sut.CreateFundoCedente(FundoId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateFundoCedente_FundoNotFound_Returns404()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns((Fundo?)null);

        var request = new CreateFundoCedenteRequest(Guid.NewGuid(), 50m, null, DateTimeOffset.UtcNow, null);
        var result = await _sut.CreateFundoCedente(FundoId, request, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateFundoCedente_ValidationException_Returns422()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        _createHandler
            .HandleAsync(Arg.Any<CreateFundoCedenteCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ValidationException(new[] { new ValidationFailure("LimitePercentual", "Required.") }));

        var request = new CreateFundoCedenteRequest(Guid.NewGuid(), null, null, DateTimeOffset.UtcNow, null);
        var result = await _sut.CreateFundoCedente(FundoId, request, CancellationToken.None);

        result.ShouldBeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task CreateFundoCedente_DuplicateActive_Returns409()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        _createHandler
            .HandleAsync(Arg.Any<CreateFundoCedenteCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateActiveAssociationException("FundoCedente", "FundoId+CedenteId already active."));

        var request = new CreateFundoCedenteRequest(Guid.NewGuid(), 50m, null, DateTimeOffset.UtcNow, null);
        var result = await _sut.CreateFundoCedente(FundoId, request, CancellationToken.None);

        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateFundoCedente_HappyPath_Returns201()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        _createHandler
            .HandleAsync(Arg.Any<CreateFundoCedenteCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleDto);

        var request = new CreateFundoCedenteRequest(Guid.NewGuid(), 50m, null, DateTimeOffset.UtcNow, null);
        var result = await _sut.CreateFundoCedente(FundoId, request, CancellationToken.None);

        var created = result.ShouldBeOfType<CreatedAtActionResult>();
        created.Value.ShouldBe(SampleDto);
        created.ActionName.ShouldBe(nameof(FundoCedentesController.GetFundoCedenteById));
    }

    // =========================================================================
    // ListFundoCedentes
    // =========================================================================

    [Fact]
    public async Task ListFundoCedentes_CrossTenantFundo_Returns404()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>())
            .Returns(MakeFundo(Guid.NewGuid()));

        var result = await _sut.ListFundoCedentes(FundoId, ct: CancellationToken.None);
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ListFundoCedentes_HappyPath_Returns200()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        var paged = new PaginatedResult<RelFundoCedenteDto>(new[] { SampleDto }, 1, 1, 20);
        _listHandler
            .HandleAsync(Arg.Any<GetFundoCedentesQuery>(), Arg.Any<CancellationToken>())
            .Returns(paged);

        var result = await _sut.ListFundoCedentes(FundoId, ct: CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(paged);
    }

    // =========================================================================
    // GetFundoCedenteById
    // =========================================================================

    [Fact]
    public async Task GetFundoCedenteById_CrossTenantFundo_Returns404()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>())
            .Returns(MakeFundo(Guid.NewGuid()));

        var result = await _sut.GetFundoCedenteById(FundoId, AssocId, CancellationToken.None);
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetFundoCedenteById_AssocNotFound_Returns404()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        _repo.GetByIdAsync(AssocId, Arg.Any<CancellationToken>()).Returns((FundoCedenteAggregate?)null);

        var result = await _sut.GetFundoCedenteById(FundoId, AssocId, CancellationToken.None);
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetFundoCedenteById_WrongFundo_Returns404()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        // Association belongs to a different FundoId
        var assoc = FundoCedenteAggregate.Create(Guid.NewGuid(), Guid.NewGuid(),
            LimiteExposicao.Create(50m, null), JanelaVigencia.Create(DateTimeOffset.UtcNow));
        _repo.GetByIdAsync(AssocId, Arg.Any<CancellationToken>()).Returns(assoc);

        var result = await _sut.GetFundoCedenteById(FundoId, AssocId, CancellationToken.None);
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetFundoCedenteById_HappyPath_Returns200()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        var assoc = FundoCedenteAggregate.Create(FundoId, Guid.NewGuid(),
            LimiteExposicao.Create(50m, null), JanelaVigencia.Create(DateTimeOffset.UtcNow));
        _repo.GetByIdAsync(AssocId, Arg.Any<CancellationToken>()).Returns(assoc);

        var result = await _sut.GetFundoCedenteById(FundoId, AssocId, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var dto = ok.Value.ShouldBeOfType<RelFundoCedenteDto>();
        dto.FundoId.ShouldBe(FundoId);
    }

    // =========================================================================
    // UpdateFundoCedenteLimits
    // =========================================================================

    [Fact]
    public async Task UpdateFundoCedenteLimits_NullBody_Returns400()
    {
        var result = await _sut.UpdateFundoCedenteLimits(FundoId, AssocId, null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateFundoCedenteLimits_CrossTenantFundo_Returns404()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>())
            .Returns(MakeFundo(Guid.NewGuid()));

        var request = new UpdateRelationshipLimitsRequest(50m, null);
        var result = await _sut.UpdateFundoCedenteLimits(FundoId, AssocId, request, CancellationToken.None);
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateFundoCedenteLimits_InvalidStateTransition_Returns400()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        _updateHandler
            .HandleAsync(Arg.Any<UpdateFundoCedenteLimiteCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidStateTransitionException("Cannot update HISTORICO association."));

        var request = new UpdateRelationshipLimitsRequest(50m, null);
        var result = await _sut.UpdateFundoCedenteLimits(FundoId, AssocId, request, CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateFundoCedenteLimits_HappyPath_Returns200()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        _updateHandler
            .HandleAsync(Arg.Any<UpdateFundoCedenteLimiteCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleDto);

        var request = new UpdateRelationshipLimitsRequest(50m, null);
        var result = await _sut.UpdateFundoCedenteLimits(FundoId, AssocId, request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(SampleDto);
    }

    // =========================================================================
    // TransitionFundoCedenteStatus
    // =========================================================================

    [Fact]
    public async Task TransitionFundoCedenteStatus_NullBody_Returns400()
    {
        var result = await _sut.TransitionFundoCedenteStatus(FundoId, AssocId, null, CancellationToken.None);
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task TransitionFundoCedenteStatus_CrossTenantFundo_Returns404()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>())
            .Returns(MakeFundo(Guid.NewGuid()));

        var request = new TransitionRelationshipStatusRequest(RelationshipStatus.INATIVO);
        var result = await _sut.TransitionFundoCedenteStatus(FundoId, AssocId, request, CancellationToken.None);
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task TransitionFundoCedenteStatus_InvalidTransition_Returns400()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        _transitionHandler
            .HandleAsync(Arg.Any<TransitionFundoCedenteStatusCommand>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidStateTransitionException("Cannot transition from HISTORICO."));

        var request = new TransitionRelationshipStatusRequest(RelationshipStatus.ATIVO);
        var result = await _sut.TransitionFundoCedenteStatus(FundoId, AssocId, request, CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task TransitionFundoCedenteStatus_HappyPath_Returns200()
    {
        _fundoRepo.GetByIdAsync(FundoId, Arg.Any<CancellationToken>()).Returns(MakeFundo());
        _transitionHandler
            .HandleAsync(Arg.Any<TransitionFundoCedenteStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(SampleDto with { Status = RelationshipStatus.INATIVO });

        var request = new TransitionRelationshipStatusRequest(RelationshipStatus.INATIVO);
        var result = await _sut.TransitionFundoCedenteStatus(FundoId, AssocId, request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var dto = ok.Value.ShouldBeOfType<RelFundoCedenteDto>();
        dto.Status.ShouldBe(RelationshipStatus.INATIVO);
    }
}
