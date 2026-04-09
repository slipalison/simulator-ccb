using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.ClientAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

public sealed class AdminRepository : IAdminRepository
{
    private readonly AppDbContext _db;

    public AdminRepository(AppDbContext db) => _db = db;

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Clients.FindAsync([id], ct);

    public async Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? search, string? status, CancellationToken ct = default)
    {
        var query = _db.Clients.AsNoTracking();

        // Status filter: deleted
        if (status?.ToLowerInvariant() == "deleted")
            query = query.Where(c => c.DeletedAt.HasValue);
        else
            query = query.Where(c => !c.DeletedAt.HasValue);

        // Search filter
        if (!string.IsNullOrWhiteSpace(search))
            query = ApplySearch(query, search);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.AsReadOnly(), totalCount);
    }

    public Task UpdateAsync(Client client, CancellationToken ct = default)
    {
        _db.Clients.Update(client);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);

    private static IQueryable<Client> ApplySearch(IQueryable<Client> query, string search)
    {
        var normalized = search.Trim().ToLowerInvariant();
        var digitsOnly = new string(normalized.Where(char.IsDigit).ToArray());

        return query.Where(c =>
            EF.Functions.ILike(c.Name, $"%{normalized}%") ||
            EF.Functions.ILike(c.Email.Value, $"%{normalized}%") ||
            (c.Cpf != null && c.Cpf.Value.Contains(normalized)) ||
            (c.Cnpj != null && c.Cnpj.Value.Contains(normalized)) ||
            (digitsOnly.Length > 0 && (
                (c.Cpf != null && c.Cpf.Value.Contains(digitsOnly)) ||
                (c.Cnpj != null && c.Cnpj.Value.Contains(digitsOnly))
            ))
        );
    }
}
