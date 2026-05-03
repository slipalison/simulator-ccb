using Microsoft.Extensions.Logging;
using NSubstitute;
using Onboarding.Application.Common;
using Onboarding.Application.Fundos.Commands;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Repositories;
using Shouldly;

namespace Onboarding.Application.Tests.Fundos.Commands;

public class UpdateConsultoriaFundoCommandHandlerTests
{
    private readonly IConsultoriaFundoRepository _repository;
    private readonly ICurrentCompanyService _currentCompanyService;
    private readonly IAuditService _auditService;
    private readonly ILogger<UpdateConsultoriaFundoCommandHandler> _logger;
    private readonly UpdateConsultoriaFundoCommandHandler _sut;

    public UpdateConsultoriaFundoCommandHandlerTests()
    {
        _repository = Substitute.For<IConsultoriaFundoRepository>();
        _currentCompanyService = Substitute.For<ICurrentCompanyService>();
        _auditService = Substitute.For<IAuditService>();
        _logger = Substitute.For<ILogger<UpdateConsultoriaFundoCommandHandler>>();

        _sut = new UpdateConsultoriaFundoCommandHandler(_repository, _currentCompanyService, _auditService, _logger);
    }

    private static UpdateConsultoriaFundoCommand ValidCommand(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        RazaoSocial: "Consultoria Atualizada",
        NomeFantasia: "Fantasia Nova",
        Email: "novo@teste.com",
        Telefone: "11888888888",
        Status: ConsultoriaFundoStatus.ATIVO,
        ActorSub: "test-sub-123",
        ActorEmail: "actor@teste.com"
    );

    [Fact]
    public async Task HandleAsync_WithValidData_UpdatesFieldsAndSaves()
    {
        // Arrange
        var consultoria = ConsultoriaFundo.Register("Nome Original", "11444777000161", Guid.NewGuid());
        var command = ValidCommand(consultoria.Id);
        _repository.GetByIdAsync(consultoria.Id, Arg.Any<CancellationToken>()).Returns(consultoria);

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.RazaoSocial.ShouldBe(command.RazaoSocial);

        await _repository.Received(1).SaveAsync(consultoria, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenEntityNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var command = ValidCommand();
        _repository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((ConsultoriaFundo?)null);

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(() => _sut.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_RecordsAuditLog()
    {
        // Arrange
        var consultoria = ConsultoriaFundo.Register("Nome Original", "11444777000161", Guid.NewGuid());
        var command = ValidCommand(consultoria.Id);
        _repository.GetByIdAsync(consultoria.Id, Arg.Any<CancellationToken>()).Returns(consultoria);

        // Act
        await _sut.HandleAsync(command);

        // Assert
        await _auditService.Received(1).RecordAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            ActionType.ConsultoriaUpdated,
            Arg.Any<Guid?>(),
            command.RazaoSocial,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}