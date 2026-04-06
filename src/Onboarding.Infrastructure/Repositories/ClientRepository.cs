using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

public sealed class ClientRepository : IClientRepository
{
    private readonly AppDbContext _db;

    public ClientRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Client client, CancellationToken ct = default)
    {
        await _db.Clients.AddAsync(client, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Clients.FindAsync([id], ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        // Normalize to lowercase to match Email value object behavior
        var normalized = email.ToLowerInvariant();
        return await _db.Clients.AnyAsync(c => c.Email.Value == normalized, ct);
    }

    public async Task<bool> ExistsByCpfAsync(string cpf, CancellationToken ct = default)
    {
        // Normalize: remove formatting characters (./-) so "529.982.247-25" → "52998224725"
        var normalized = cpf.Replace(".", "").Replace("-", "");
        return await _db.Clients.AnyAsync(c => c.Cpf != null && c.Cpf.Value == normalized, ct);
    }

    public async Task<bool> ExistsByCnpjAsync(string cnpj, CancellationToken ct = default)
    {
        // Normalize: remove formatting characters for CNPJ "11.222.333/0001-81" → "11222333000181"
        var normalized = cnpj.Replace(".", "").Replace("/", "").Replace("-", "");
        return await _db.Clients.AnyAsync(c => c.Cnpj != null && c.Cnpj.Value == normalized, ct);
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
}
