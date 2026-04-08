using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.PasswordReset;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _dbContext;

    public PasswordResetTokenRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        await _dbContext.PasswordResetTokens.AddAsync(token, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        return await _dbContext.PasswordResetTokens
            .FirstOrDefaultAsync(x => x.Token == token, ct);
    }

    public async Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        _dbContext.PasswordResetTokens.Update(token);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<int> CountRecentTokensAsync(string email, TimeSpan window, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(window);
        return await _dbContext.PasswordResetTokens
            .CountAsync(x => x.Email == email && x.CreatedAt >= cutoff, ct);
    }
}
