using Onboarding.Domain.Aggregates.PasswordReset;

namespace Onboarding.Domain.Repositories;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken ct = default);
    Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default);
    Task<int> CountRecentTokensAsync(string email, TimeSpan window, CancellationToken ct = default);
}
