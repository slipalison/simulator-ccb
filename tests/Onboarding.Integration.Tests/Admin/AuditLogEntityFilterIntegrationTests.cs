using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Repositories;
using Onboarding.Integration.Tests.Fixtures;
using Shouldly;

namespace Onboarding.Integration.Tests.Admin;

/// <summary>
/// Integration tests for AdminAuditLog entity filter — Phase 52 T-1.
///
/// Verifies the full round-trip:
///   1. Write an AdminAuditLog row with EntityType + EntityId via IAdminAuditLogRepository.
///   2. Query via GET /api/admin/audit-log?entityType=X&entityId=Y.
///   3. Assert only matching rows are returned.
///   4. Assert unfiltered query returns all rows (backward-compat).
///
/// Uses Testcontainers PostgreSQL — requires Docker. Tagged [Category=Integration].
/// </summary>
[Trait("Category", "Integration")]
public sealed class AuditLogEntityFilterIntegrationTests : PostgreSqlIntegrationTestBase, IClassFixture<PostgreSqlFixture>
{
    public AuditLogEntityFilterIntegrationTests(PostgreSqlFixture fixture) : base(fixture) { }

    // =========================================================================
    // HTTP client helpers
    // =========================================================================

    private HttpClient CreateAdminClient() => CreateAdminJwt("audit-log-admin-sub");

    // =========================================================================
    // Seed helpers
    // =========================================================================

    private async Task SeedAuditRowAsync(string entityType, Guid entityId, string? altEntityType = null, Guid? altEntityId = null)
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAdminAuditLogRepository>();

        var log1 = AdminAuditLog.Create(
            Guid.NewGuid(), "admin@test.com", ActionType.FundoStatusChanged,
            entityId, "FundoAlpha", "RASCUNHO -> ATIVO",
            entityType: entityType, entityId: entityId);
        await repo.AddAsync(log1);

        // Legacy row without entityType — should not appear in filtered query but in unfiltered
        var legacyLog = AdminAuditLog.Create(
            Guid.NewGuid(), "admin@test.com", ActionType.AdminCreated,
            Guid.NewGuid(), "OtherUser", "legacy entry");
        await repo.AddAsync(legacyLog);

        if (altEntityType is not null && altEntityId.HasValue)
        {
            var log2 = AdminAuditLog.Create(
                Guid.NewGuid(), "admin@test.com", ActionType.RelFundoCedenteStatusChanged,
                altEntityId.Value, "FundoCedenteX", "ATIVO -> SUSPENSO",
                entityType: altEntityType, entityId: altEntityId.Value);
            await repo.AddAsync(log2);
        }

        await repo.SaveChangesAsync();
    }

    // =========================================================================
    // T-1 round-trip: write then filter by entityType + entityId → 200 filtered
    // =========================================================================

    [Fact]
    public async Task GetAuditLog_FilterByEntityTypeAndEntityId_ReturnsOnlyMatchingRow()
    {
        // Arrange — seed a Fundo audit row and an unrelated row
        var fundoId = Guid.NewGuid();
        await SeedAuditRowAsync("Fundo", fundoId);

        using var client = CreateAdminClient();

        // Act — filter by both entityType + entityId
        var response = await client.GetAsync(
            $"/api/admin/audit-log?entityType=Fundo&entityId={fundoId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResultDto>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(1);
        result.Items.ShouldNotBeNull();
        result.Items.Length.ShouldBe(1);
        result.Items[0].EntityType.ShouldBe("Fundo");
        result.Items[0].EntityId.ShouldBe(fundoId);
    }

    [Fact]
    public async Task GetAuditLog_FilterByEntityTypeOnly_ReturnsAllRowsWithThatType()
    {
        // Arrange — seed two Fundo rows (different IDs) + one FundoCedente row
        var fundoId1 = Guid.NewGuid();
        var fundoCedenteId = Guid.NewGuid();
        await SeedAuditRowAsync("Fundo", fundoId1, "FundoCedente", fundoCedenteId);

        using var client = CreateAdminClient();

        // Act — filter only by entityType=Fundo
        var response = await client.GetAsync("/api/admin/audit-log?entityType=Fundo");

        // Assert — only Fundo row(s) returned
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResultDto>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        result.ShouldNotBeNull();
        result.Items.ShouldNotBeNull();
        result.Items!.ShouldAllBe(i => i.EntityType == "Fundo");
    }

    [Fact]
    public async Task GetAuditLog_NoFilter_BackwardCompat_ReturnsAllRows()
    {
        // Arrange — seed rows with and without entityType
        var fundoId = Guid.NewGuid();
        await SeedAuditRowAsync("Fundo", fundoId);

        using var client = CreateAdminClient();

        // Act — no entity filter
        var response = await client.GetAsync("/api/admin/audit-log");

        // Assert — response includes both scoped and legacy rows
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResultDto>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        result.ShouldNotBeNull();
        // At least 2 rows: the seeded Fundo row + the legacy row (no entityType)
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetAuditLog_FilterByEntityId_WithNonExistentId_ReturnsEmpty()
    {
        // Arrange — no rows for this ID
        var unknownId = Guid.NewGuid();
        using var client = CreateAdminClient();

        // Act
        var response = await client.GetAsync(
            $"/api/admin/audit-log?entityType=Fundo&entityId={unknownId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedResultDto>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(0);
        result.Items!.Length.ShouldBe(0);
    }

    [Fact]
    public async Task GetAuditLog_WithoutAdminBearer_Returns401()
    {
        using var client = CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/admin/audit-log?entityType=Fundo");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // JSON DTO projection types for deserialization (self-contained)
    // =========================================================================

    private sealed class PaginatedResultDto
    {
        public AuditLogItemDto[]? Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private sealed class AuditLogItemDto
    {
        public Guid Id { get; set; }
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public string? ActionType { get; set; }
        public string? AdminUserName { get; set; }
    }
}
