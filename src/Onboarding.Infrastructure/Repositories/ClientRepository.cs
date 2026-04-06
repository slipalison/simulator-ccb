using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
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
}
