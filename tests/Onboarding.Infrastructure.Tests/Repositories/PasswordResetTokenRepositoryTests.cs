using Onboarding.Domain.Aggregates.PasswordReset;
using Onboarding.Infrastructure.Repositories;
using Shouldly;

namespace Onboarding.Infrastructure.Tests.Repositories;

/// <summary>
/// InMemory unit tests for PasswordResetTokenRepository.
/// Covers: AddAsync, GetByTokenAsync, UpdateAsync, CountRecentTokensAsync.
/// </summary>
public sealed class PasswordResetTokenRepositoryTests
{
    [Fact]
    public async Task AddAsync_ValidToken_PersistsToken()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new PasswordResetTokenRepository(db);
        var token = PasswordResetToken.Create("user@test.com");

        await repo.AddAsync(token);

        var found = await repo.GetByTokenAsync(token.Token);
        found.ShouldNotBeNull();
        found!.Email.ShouldBe("user@test.com");
        found.IsUsed.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByTokenAsync_ExistingToken_ReturnsToken()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new PasswordResetTokenRepository(db);
        var token = PasswordResetToken.Create("a@b.com");
        await repo.AddAsync(token);

        var result = await repo.GetByTokenAsync(token.Token);

        result.ShouldNotBeNull();
        result!.Token.ShouldBe(token.Token);
    }

    [Fact]
    public async Task GetByTokenAsync_UnknownToken_ReturnsNull()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new PasswordResetTokenRepository(db);

        var result = await repo.GetByTokenAsync("does-not-exist");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_MarkAsUsed_PersistsChange()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new PasswordResetTokenRepository(db);
        var token = PasswordResetToken.Create("upd@test.com");
        await repo.AddAsync(token);

        token.MarkAsUsed();
        await repo.UpdateAsync(token);

        var result = await repo.GetByTokenAsync(token.Token);
        result.ShouldNotBeNull();
        result!.IsUsed.ShouldBeTrue();
    }

    [Fact]
    public async Task CountRecentTokensAsync_WithinWindow_CountsCorrectly()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new PasswordResetTokenRepository(db);
        var email = "count@test.com";

        // Add 3 tokens for the same email
        for (var i = 0; i < 3; i++)
        {
            await repo.AddAsync(PasswordResetToken.Create(email));
        }
        // Add 1 for different email
        await repo.AddAsync(PasswordResetToken.Create("other@test.com"));

        var count = await repo.CountRecentTokensAsync(email, TimeSpan.FromMinutes(30));

        count.ShouldBe(3);
    }

    [Fact]
    public async Task CountRecentTokensAsync_NoTokens_ReturnsZero()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var repo = new PasswordResetTokenRepository(db);

        var count = await repo.CountRecentTokensAsync("nobody@test.com", TimeSpan.FromMinutes(15));

        count.ShouldBe(0);
    }
}
