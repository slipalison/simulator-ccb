using Microsoft.EntityFrameworkCore;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates.Audit;
using Onboarding.Domain.Aggregates.CedenteAggregate;
using Onboarding.Domain.Aggregates.CedenteTipoAtivoAggregate;
using Onboarding.Domain.Aggregates.CompanyAggregate;
using Onboarding.Domain.Aggregates.ConsultoriaFundoAggregate;
using Onboarding.Domain.Aggregates.CustodianteAggregate;
using Onboarding.Domain.Aggregates.EmployeeAggregate;
using Onboarding.Domain.Aggregates.FundoCedenteAggregate;
using Onboarding.Domain.Aggregates.FundoAggregate;
using Onboarding.Domain.Aggregates.FundoTipoAtivoAggregate;
using Onboarding.Domain.Aggregates.PasswordReset;
using Onboarding.Domain.Aggregates.TipoAtivoAggregate;
using Onboarding.Infrastructure.Persistence.Configurations;

namespace Onboarding.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly ICurrentCompanyService _currentCompanyService;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentCompanyService currentCompanyService) : base(options)
    {
        _currentCompanyService = currentCompanyService;
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AccessGroup> AccessGroups => Set<AccessGroup>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    // Fundos module — 5 aggregate root DbSets (Phase 46)
    // FundoCedente, FundoTipoAtivo, CedenteTipoAtivo are owned via OwnsMany — no separate DbSets
    public DbSet<Fundo> Fundos => Set<Fundo>();
    public DbSet<ConsultoriaFundo> ConsultoriasFundo => Set<ConsultoriaFundo>();
    public DbSet<Custodiante> Custodiantes => Set<Custodiante>();
    public DbSet<Cedente> Cedentes => Set<Cedente>();
    public DbSet<TipoAtivo> TiposAtivo => Set<TipoAtivo>();

    // Phase 50 — three standalone relationship aggregates (D-21)
    // These are independent aggregates with full lifecycle, distinct from the
    // legacy OwnsMany join entities (FundoCedente, FundoTipoAtivo, CedenteTipoAtivo)
    // that remain inside their parent aggregates for backward compatibility.
    public DbSet<FundoCedenteAggregate> RelFundoCedentes => Set<FundoCedenteAggregate>();
    public DbSet<CedenteTipoAtivoAggregate> RelCedenteTiposAtivo => Set<CedenteTipoAtivoAggregate>();
    public DbSet<FundoTipoAtivoAggregate> RelFundoTiposAtivo => Set<FundoTipoAtivoAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CompanyConfiguration(_currentCompanyService));
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration(_currentCompanyService));
        modelBuilder.ApplyConfiguration(new AccessGroupConfiguration(_currentCompanyService));
        modelBuilder.ApplyConfiguration(new PasswordResetTokenConfiguration());
        modelBuilder.ApplyConfiguration(new AdminAuditLogConfiguration());

        // Fundos module configurations (Phase 46)
        modelBuilder.ApplyConfiguration(new FundoConfiguration(_currentCompanyService));
        modelBuilder.ApplyConfiguration(new ConsultoriaFundoConfiguration(_currentCompanyService));
        modelBuilder.ApplyConfiguration(new CustodianteConfiguration(_currentCompanyService));
        modelBuilder.ApplyConfiguration(new CedenteConfiguration(_currentCompanyService));
        modelBuilder.ApplyConfiguration(new TipoAtivoConfiguration()); // NO ICurrentCompanyService — global entity (D-03/TEN-03)

        // Phase 50 — standalone relationship aggregate configurations (D-21)
        // NO ICurrentCompanyService — tenant scoping via parent aggregate (D-5, HasQueryFilter not applied here)
        modelBuilder.ApplyConfiguration(new FundoCedenteAggregateConfiguration());
        modelBuilder.ApplyConfiguration(new CedenteTipoAtivoAggregateConfiguration());
        modelBuilder.ApplyConfiguration(new FundoTipoAtivoAggregateConfiguration());
    }
}