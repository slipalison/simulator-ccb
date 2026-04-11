using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// Thin EF Core repository wrapper — tested via integration tests.
/// Excluded from unit test coverage as it contains no business logic.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ClientRepository : IClientRepository
{
    private readonly AppDbContext _db;

    public ClientRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Client client, CancellationToken ct = default)
    {
        await _db.Clients.AddAsync(client, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(Client client, CancellationToken ct = default)
    {
        _db.Clients.Update(client);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Clients.FindAsync([id], ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        var emailVo = Email.Create(email);
        return await _db.Clients.AnyAsync(c => c.Email == emailVo, ct);
    }

    public async Task<bool> ExistsByCpfAsync(string cpf, CancellationToken ct = default)
    {
        var cpfVo = Cpf.Create(cpf);
        return await _db.Clients.AnyAsync(c => c.Cpf == cpfVo, ct);
    }

    public async Task<bool> ExistsByCnpjAsync(string cnpj, CancellationToken ct = default)
    {
        var cnpjVo = Cnpj.Create(cnpj);
        return await _db.Clients.AnyAsync(c => c.Cnpj == cnpjVo, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // Compensation step (REG-06): called when Keycloak user creation fails after app_db persist.
        // FindAsync + Remove respects change tracking (no ExecuteDeleteAsync bypass).
        var client = await _db.Clients.FindAsync([id], ct);
        if (client is not null)
        {
            _db.Clients.Remove(client);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<Client?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        // Normalize to lowercase to match Email value object behavior (same as ExistsByEmailAsync).
        // Email.Create() calls ToLowerInvariant() internally — explicit here for clarity.
        var normalized = email.ToLowerInvariant();
        var emailVo = Email.Create(normalized);
        return await _db.Clients
            .FirstOrDefaultAsync(c => c.Email == emailVo, ct);
    }

    public async Task<Client?> GetByKeycloakSubAsync(string keycloakSub, CancellationToken ct = default)
    {
        return await _db.Clients
            .FirstOrDefaultAsync(c => c.KeycloakUserId == keycloakSub, ct);
    }

    public async Task<Client?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _db.Clients
            .FirstOrDefaultAsync(c => c.Name == name && c.DeletedAt == null, ct);
    }

    public Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, string? status, CancellationToken ct = default)
    {
        throw new NotImplementedException("Use IAdminRepository.GetPagedAsync instead.");
    }
}
