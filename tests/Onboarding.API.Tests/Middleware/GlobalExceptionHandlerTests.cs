using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Onboarding.API.Tests.Authentication;
using Onboarding.Application.Common;
using Onboarding.Application.Services;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Exceptions;
using Onboarding.Domain.Repositories;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;

namespace Onboarding.API.Tests.Middleware;

/// <summary>
/// Tests for GlobalExceptionHandler middleware — verifies that unhandled exceptions from controllers
/// are mapped to correct HTTP status codes without leaking stack traces.
/// Tests use the CompaniesController (GET /api/companies/me) which throws through the repository mock.
/// </summary>
[Collection(WebAppFactoryCollection.Name)]
public class GlobalExceptionHandlerTests : IAsyncLifetime
{
    private AuthTestApiFactory? _factory;
    private HttpClient? _client;

    public Task InitializeAsync()
    {
        _factory = new AuthTestApiFactory();
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
    }

    private HttpRequestMessage AuthenticatedGet(string url)
    {
        var jwt = FakeJwtTokenHelper.GenerateFakeJwt("test@example.com", "test-sub");
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return request;
    }

    [Fact]
    public async Task UnhandledException_Returns500_WithoutStackTrace()
    {
        _factory!.RepositoryMock
            .GetByKeycloakSubAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Simulated internal error"));

        var response = await _client!.SendAsync(AuthenticatedGet("/api/companies/me"));

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain("Simulated internal error");
        body.ShouldNotContain("StackTrace");
        body.ShouldContain("unexpected error");
    }

    [Fact]
    public async Task KeyNotFoundException_Returns404()
    {
        _factory!.RepositoryMock
            .GetByKeycloakSubAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Entity not found"));

        var response = await _client!.SendAsync(AuthenticatedGet("/api/companies/me"));
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnauthorizedAccessException_Returns403()
    {
        _factory!.RepositoryMock
            .GetByKeycloakSubAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        var response = await _client!.SendAsync(AuthenticatedGet("/api/companies/me"));
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DuplicateEntityException_Returns409Conflict()
    {
        _factory!.RepositoryMock
            .GetByKeycloakSubAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEntityException("Company", "12.345.678/0001-90"));

        var response = await _client!.SendAsync(AuthenticatedGet("/api/companies/me"));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("already exists");
    }

    [Fact]
    public async Task InvalidStateTransitionException_Returns400BadRequest()
    {
        _factory!.RepositoryMock
            .GetByKeycloakSubAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidStateTransitionException("Fundo", FundoStatus.ATIVO, FundoStatus.RASCUNHO));

        var response = await _client!.SendAsync(AuthenticatedGet("/api/companies/me"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("cannot transition from");
    }
}
