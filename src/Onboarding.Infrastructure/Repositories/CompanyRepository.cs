using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Repositories;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Persistence;

namespace Onboarding.Infrastructure.Repositories;

/// <summary>
/// Thin EF Core repository wrapper — tested via integration tests.
/// Excluded from unit test coverage as it contains no business logic.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CompanyRepository : ICompanyRepository
{
    private readonly AppDbContext _db;

    public CompanyRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Company company, CancellationToken ct = default)
    {
        await _db.Companies.AddAsync(company, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(Company company, CancellationToken ct = default)
    {
        _db.Companies.Update(company);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Companies.FindAsync([id], ct);

    public async Task<bool> ExistsByCnpjAsync(string cnpj, CancellationToken ct = default)
    {
        var cnpjVo = Cnpj.Create(cnpj);
        return await _db.Companies.AnyAsync(c => c.Cnpj == cnpjVo, ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        var emailVo = Email.Create(email);
        return await _db.Companies.AnyAsync(c => c.Email == emailVo, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var company = await _db.Companies.FindAsync([id], ct);
        if (company is not null)
        {
            _db.Companies.Remove(company);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<Company?> GetByKeycloakSubAsync(string keycloakSub, CancellationToken ct = default)
    {
        return await _db.Companies
            .FirstOrDefaultAsync(c => c.KeycloakUserId == keycloakSub, ct);
    }

    public async Task<Company?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.ToLowerInvariant();
        var emailVo = Email.Create(normalized);
        return await _db.Companies
            .FirstOrDefaultAsync(c => c.Email == emailVo, ct);
    }
}