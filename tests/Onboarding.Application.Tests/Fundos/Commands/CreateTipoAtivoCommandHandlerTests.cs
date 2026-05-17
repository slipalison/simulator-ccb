using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Commands;

public class CreateTipoAtivoCommandHandlerTests
{
    private readonly ITipoAtivoRepository _repository;
    private readonly IAuditService _auditService;
    private readonly ILogger<CreateTipoAtivoCommandHandler> _logger;
    private readonly CreateTipoAtivoCommandHandler _sut;

    public CreateTipoAtivoCommandHandlerTests()
    {
        _repository = Substitute.For<ITipoAtivoRepository>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<CreateTipoAtivoCommandHandler>>();

        _sut = new CreateTipoAtivoCommandHandler(_repository, _auditService, _logger);
    }

    private static CreateTipoAtivoCommand ValidCommand() => new(
        Codigo: "RF-001",
        Descricao: "Título Público Federal",
        Categoria: TipoAtivoCategoria.RendaFixa,
        Subcategoria: "TPF",
        OrdemExibicao: 1
    );

    [Fact]
    public async Task HandleAsync_WithValidData_CreatesTipoAtivoGlobally()
    {
        // Arrange
        var command = ValidCommand();
        _repository.ExistsByCodigoAsync(command.Codigo, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.Codigo.ShouldBe(command.Codigo);
        result.Descricao.ShouldBe(command.Descricao);
        result.Categoria.ShouldBe(TipoAtivoCategoria.RendaFixa);
        result.Status.ShouldBe(TipoAtivoStatus.ATIVO);

        await _repository.Received(1).AddAsync(Arg.Is<TipoAtivo>(t =>
            t.Codigo == command.Codigo), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateCodigo_ThrowsDuplicateEntityException()
    {
        // Arrange
        var command = ValidCommand();
        _repository.ExistsByCodigoAsync(command.Codigo, Arg.Any<CancellationToken>()).Returns(true);

        // Act & Assert
        var ex = await Should.ThrowAsync<DuplicateEntityException>(() => _sut.HandleAsync(command));
        ex.EntityType.ShouldBe("TipoAtivo");
        ex.KeyValue.ShouldBe(command.Codigo);

        await _repository.DidNotReceive().AddAsync(Arg.Any<TipoAtivo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RecordsAuditLog()
    {
        // Arrange
        var command = ValidCommand();
        _repository.ExistsByCodigoAsync(command.Codigo, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _auditService.Received(1).RecordAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            ActionType.TipoAtivoCreated,
            Arg.Any<Guid?>(),
            command.Descricao,
            Arg.Is<string>(d => d.Contains(command.Codigo)),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DoesNotInjectCurrentCompanyService_GlobalScope()
    {
        // This test verifies the handler does NOT use ICurrentCompanyService.
        // The handler constructor only takes repository + audit + logger (no ICurrentCompanyService).
        // This is a design contract test — if someone adds ICurrentCompanyService,
        // this test will need updating.
        var handler = new CreateTipoAtivoCommandHandler(_repository, _auditService, _logger);
        handler.ShouldNotBeNull();

        // Additionally: the repository call should NOT have companyId parameter
        var command = ValidCommand();
        _repository.ExistsByCodigoAsync(command.Codigo, Arg.Any<CancellationToken>()).Returns(false);

        await handler.HandleAsync(command);

        // Verify ExistsByCodigoAsync was called WITHOUT a companyId parameter (global scope)
        await _repository.Received(1).ExistsByCodigoAsync(command.Codigo, Arg.Any<CancellationToken>());
    }
}