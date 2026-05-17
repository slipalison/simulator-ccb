using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Application.Admin.Commands;
using Onboarding.Application.Admin.DTOs;
using Onboarding.Application.Admin.Queries;
using Onboarding.Application.Admin.Validators;
using Onboarding.Application.Auth.Commands;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Auth.Validators;
using Onboarding.Application.Common;
using Onboarding.Application.Companies.Commands;
using Onboarding.Application.Companies.DTOs;
using Onboarding.Application.Companies.Queries;
using Onboarding.Application.Fundos.Commands;
using Onboarding.Application.Fundos.Commands.CreateCedenteTipoAtivo;
using Onboarding.Application.Fundos.Commands.CreateFundoCedente;
using Onboarding.Application.Fundos.Commands.CreateFundoTipoAtivo;
using Onboarding.Application.Fundos.Commands.TransitionCedenteTipoAtivoStatus;
using Onboarding.Application.Fundos.Commands.TransitionFundoCedenteStatus;
using Onboarding.Application.Fundos.Commands.TransitionFundoTipoAtivoStatus;
using Onboarding.Application.Fundos.Commands.UpdateCedenteTipoAtivoLimite;
using Onboarding.Application.Fundos.Commands.UpdateFundoCedenteLimite;
using Onboarding.Application.Fundos.Commands.UpdateFundoTipoAtivoLimite;
using Onboarding.Application.Fundos.DTOs;
using Onboarding.Application.Fundos.Queries;
using Onboarding.Application.Fundos.Queries.GetCedenteTiposAtivos;
using Onboarding.Application.Fundos.Queries.GetFundoCedentes;
using Onboarding.Application.Fundos.Queries.GetFundoTiposAtivos;
namespace Onboarding.Application;

/// <summary>
/// Application layer DI registration — excluded from coverage as it's configuration code.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Company registration commands (Phase 38 — REG-01, REG-02)
        services.AddScoped<ICommandHandler<RegisterCompanyCommand, RegisterCompanyResult>, RegisterCompanyCommandHandler>();
        services.AddScoped<IValidator<RegisterCompanyCommand>, RegisterCompanyCommandValidator>();

        // Employee registration & management commands (Phase 38 — REG-03, MGMT-01..05)
        // These are Companies-scoped commands (company isolation via CompanyId), different from Admin stubs
        services.AddScoped<ICommandHandler<RegisterEmployeeCommand, RegisterEmployeeResult>, RegisterEmployeeCommandHandler>();
        services.AddScoped<IValidator<RegisterEmployeeCommand>, RegisterEmployeeCommandValidator>();
        services.AddScoped<ICommandHandler<ToggleEmployeeStatusCommand, Unit>, ToggleEmployeeStatusCommandHandler>();
        services.AddScoped<ICommandHandler<ResetEmployeePasswordCommand, ResetEmployeePasswordResult>, ResetEmployeePasswordCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateEmployeeCommand, Unit>, UpdateEmployeeCommandHandler>();
        services.AddScoped<ICommandHandler<Companies.Commands.DeleteEmployeeCommand, Unit>, Companies.Commands.DeleteEmployeeCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeEmployeeAccessGroupCommand, Unit>, ChangeEmployeeAccessGroupCommandHandler>();

        // Access group CRUD commands (Phase 44 — PERM-06)
        services.AddScoped<ICommandHandler<CreateAccessGroupCommand, AccessGroupDto>, CreateAccessGroupCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateAccessGroupCommand, AccessGroupDto>, UpdateAccessGroupCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteAccessGroupCommand, Unit>, DeleteAccessGroupCommandHandler>();

        // Employee listing query (Phase 38 — MGMT-02)
        services.AddScoped<IQueryHandler<GetCompanyEmployeesQuery, PaginatedResult<EmployeeListItemDto>>, GetCompanyEmployeesQueryHandler>();

        // Auth commands (Phase 6 — AUTH-02, AUTH-04)
        services.AddScoped<ICommandHandler<LoginCommand, TokenResponse>, LoginCommandHandler>();
        services.AddScoped<ICommandHandler<RefreshTokenCommand, TokenResponse>, RefreshTokenCommandHandler>();
        services.AddScoped<IValidator<LoginCommand>, LoginCommandValidator>();
        services.AddScoped<IValidator<RefreshTokenCommand>, RefreshTokenCommandValidator>();

        // Forgot/Reset password commands (Phase 11 — UX-05)
        services.AddScoped<ICommandHandler<ForgotPasswordCommand, Unit>, ForgotPasswordCommandHandler>();
        services.AddScoped<ICommandHandler<ResetPasswordCommand, Unit>, ResetPasswordCommandHandler>();
        services.AddScoped<IValidator<ForgotPasswordCommand>, ForgotPasswordCommandValidator>();
        services.AddScoped<IValidator<ResetPasswordCommand>, ResetPasswordCommandValidator>();

        // Admin queries — Company/Employee (Phase 37 — D-19)
        services.AddScoped<IQueryHandler<GetPaginatedCompaniesQuery, PaginatedResult<CompanySummaryDto>>, GetPaginatedCompaniesHandler>();
        services.AddScoped<IQueryHandler<GetCompanyDetailsQuery, CompanySummaryDto>, GetCompanyDetailsHandler>();
        services.AddScoped<IQueryHandler<GetPaginatedEmployeesQuery, PaginatedResult<EmployeeSummaryDto>>, GetPaginatedEmployeesHandler>();
        services.AddScoped<IQueryHandler<GetEmployeeDetailsQuery, EmployeeSummaryDto>, GetEmployeeDetailsHandler>();

        // Admin commands — Company (Phase 38) + Employee (Phase 38 — MGMT-03, MGMT-05)
        services.AddScoped<ICommandHandler<UpdateCompanyCommand, Unit>, UpdateCompanyCommandHandler>();
        services.AddScoped<ICommandHandler<Admin.Commands.DeleteEmployeeCommand, Unit>, Admin.Commands.DeleteEmployeeCommandHandler>();
        services.AddScoped<ICommandHandler<BlockEmployeeCommand, Unit>, BlockEmployeeCommandHandler>();
        services.AddScoped<ICommandHandler<UnblockEmployeeCommand, Unit>, UnblockEmployeeCommandHandler>();

        // Admin management commands (Phase 29 — V5.0-01, V5.0-02)
        services.AddScoped<ICommandHandler<CreateAdminCommand, CreateAdminResult>, CreateAdminCommandHandler>();
        services.AddScoped<ICommandHandler<ForcePasswordChangeCommand, Unit>, ForcePasswordChangeCommandHandler>();
        services.AddScoped<IValidator<CreateAdminCommand>, CreateAdminCommandValidator>();
        services.AddScoped<IValidator<ForcePasswordChangeCommand>, ForcePasswordChangeCommandValidator>();

        // Audit log query (Phase 29 — V5.0-03)
        services.AddScoped<IQueryHandler<GetAuditLogQuery, PaginatedResult<AdminAuditLogDto>>, GetAuditLogQueryHandler>();

        // Admin administrators query (Phase 30 — ADM-04)
        services.AddScoped<
            IQueryHandler<GetAdministratorsQuery, IReadOnlyList<AdminUserDto>>,
            GetAdministratorsQueryHandler>();

        // Admin management (Phase 35 — MGMT-01..06, SEC-01..05, AUD-04..06)
        services.AddScoped<
            IQueryHandler<GetPaginatedAdministratorsQuery, PaginatedResult<AdminUserDto>>,
            GetPaginatedAdministratorsQueryHandler>();
        services.AddScoped<ICommandHandler<UpdateAdministratorCommand, Unit>, UpdateAdministratorCommandHandler>();
        services.AddScoped<ICommandHandler<ResetAdministratorPasswordCommand, ResetAdministratorPasswordResult>, ResetAdministratorPasswordCommandHandler>();
        services.AddScoped<ICommandHandler<ToggleAdministratorStatusCommand, Unit>, ToggleAdministratorStatusCommandHandler>();
        services.AddScoped<IValidator<UpdateAdministratorCommand>, UpdateAdministratorCommandValidator>();
        services.AddScoped<IValidator<ResetAdministratorPasswordCommand>, ResetAdministratorPasswordCommandValidator>();
        services.AddScoped<IValidator<ToggleAdministratorStatusCommand>, ToggleAdministratorStatusCommandValidator>();

        // Fund CRUD commands (Phase 47 — CAD-01..21, ADM-04)
        // ConsultoriaFundo (company-scoped per D-01)
        services.AddScoped<ICommandHandler<RegisterConsultoriaFundoCommand, ConsultoriaFundoDto>, RegisterConsultoriaFundoCommandHandler>();
        services.AddScoped<IValidator<RegisterConsultoriaFundoCommand>, RegisterConsultoriaFundoCommandValidator>();
        services.AddScoped<ICommandHandler<UpdateConsultoriaFundoCommand, ConsultoriaFundoDto>, UpdateConsultoriaFundoCommandHandler>();
        services.AddScoped<IValidator<UpdateConsultoriaFundoCommand>, UpdateConsultoriaFundoCommandValidator>();

        // Custodiante (company-scoped per D-01)
        services.AddScoped<ICommandHandler<RegisterCustodianteCommand, CustodianteDto>, RegisterCustodianteCommandHandler>();
        services.AddScoped<IValidator<RegisterCustodianteCommand>, RegisterCustodianteCommandValidator>();
        services.AddScoped<ICommandHandler<UpdateCustodianteCommand, CustodianteDto>, UpdateCustodianteCommandHandler>();
        services.AddScoped<IValidator<UpdateCustodianteCommand>, UpdateCustodianteCommandValidator>();

        // Fundo (company-scoped per D-01, state machine D-02)
        services.AddScoped<ICommandHandler<RegisterFundoCommand, FundoDto>, RegisterFundoCommandHandler>();
        services.AddScoped<IValidator<RegisterFundoCommand>, RegisterFundoCommandValidator>();
        services.AddScoped<ICommandHandler<UpdateFundoCommand, FundoDto>, UpdateFundoCommandHandler>();
        services.AddScoped<IValidator<UpdateFundoCommand>, UpdateFundoCommandValidator>();
        services.AddScoped<ICommandHandler<TransitionFundoStatusCommand, FundoDto>, TransitionFundoStatusCommandHandler>();
        services.AddScoped<IValidator<TransitionFundoStatusCommand>, TransitionFundoStatusCommandValidator>();

        // Cedente (company-scoped per D-01, PF/PJ polymorphic via CedenteDocumento D-05/D-06)
        services.AddScoped<ICommandHandler<RegisterCedentePfCommand, CedenteDto>, RegisterCedentePfCommandHandler>();
        services.AddScoped<IValidator<RegisterCedentePfCommand>, RegisterCedentePfCommandValidator>();
        services.AddScoped<ICommandHandler<RegisterCedentePjCommand, CedenteDto>, RegisterCedentePjCommandHandler>();
        services.AddScoped<IValidator<RegisterCedentePjCommand>, RegisterCedentePjCommandValidator>();
        services.AddScoped<ICommandHandler<UpdateCedenteCommand, CedenteDto>, UpdateCedenteCommandHandler>();
        services.AddScoped<IValidator<UpdateCedenteCommand>, UpdateCedenteCommandValidator>();

        // TipoAtivo (global per D-03/TEN-03 — no company scope)
        services.AddScoped<ICommandHandler<CreateTipoAtivoCommand, TipoAtivoDto>, CreateTipoAtivoCommandHandler>();
        services.AddScoped<IValidator<CreateTipoAtivoCommand>, CreateTipoAtivoCommandValidator>();
        services.AddScoped<ICommandHandler<UpdateTipoAtivoCommand, TipoAtivoDto>, UpdateTipoAtivoCommandHandler>();
        services.AddScoped<IValidator<UpdateTipoAtivoCommand>, UpdateTipoAtivoCommandValidator>();

        // Fund CRUD queries (Phase 47 — CAD-02, CAD-06, CAD-10, CAD-16, CAD-20)
        services.AddScoped<IQueryHandler<ListConsultoriaFundoQuery, PaginatedResult<ConsultoriaFundoDto>>, ListConsultoriaFundoQueryHandler>();
        services.AddScoped<IQueryHandler<ListCustodianteQuery, PaginatedResult<CustodianteDto>>, ListCustodianteQueryHandler>();
        services.AddScoped<IQueryHandler<ListFundoQuery, PaginatedResult<FundoDto>>, ListFundoQueryHandler>();
        services.AddScoped<IQueryHandler<ListCedenteQuery, PaginatedResult<CedenteDto>>, ListCedenteQueryHandler>();
        services.AddScoped<IQueryHandler<ListTipoAtivoQuery, PaginatedResult<TipoAtivoDto>>, ListTipoAtivoQueryHandler>();

        // Phase 50 — relationship aggregate commands + queries (D-21)
        // FundoCedente
        services.AddScoped<ICommandHandler<CreateFundoCedenteCommand, RelFundoCedenteDto>, CreateFundoCedenteHandler>();
        services.AddScoped<ICommandHandler<UpdateFundoCedenteLimiteCommand, RelFundoCedenteDto>, UpdateFundoCedenteLimiteHandler>();
        services.AddScoped<ICommandHandler<TransitionFundoCedenteStatusCommand, RelFundoCedenteDto>, TransitionFundoCedenteStatusHandler>();
        services.AddScoped<IQueryHandler<GetFundoCedentesQuery, PaginatedResult<RelFundoCedenteDto>>, GetFundoCedentesQueryHandler>();

        // CedenteTipoAtivo
        services.AddScoped<ICommandHandler<CreateCedenteTipoAtivoCommand, RelCedenteTipoAtivoDto>, CreateCedenteTipoAtivoHandler>();
        services.AddScoped<ICommandHandler<UpdateCedenteTipoAtivoLimiteCommand, RelCedenteTipoAtivoDto>, UpdateCedenteTipoAtivoLimiteHandler>();
        services.AddScoped<ICommandHandler<TransitionCedenteTipoAtivoStatusCommand, RelCedenteTipoAtivoDto>, TransitionCedenteTipoAtivoStatusHandler>();
        services.AddScoped<IQueryHandler<GetCedenteTiposAtivosQuery, PaginatedResult<RelCedenteTipoAtivoDto>>, GetCedenteTiposAtivosQueryHandler>();

        // FundoTipoAtivo
        services.AddScoped<ICommandHandler<CreateFundoTipoAtivoCommand, RelFundoTipoAtivoDto>, CreateFundoTipoAtivoHandler>();
        services.AddScoped<ICommandHandler<UpdateFundoTipoAtivoLimiteCommand, RelFundoTipoAtivoDto>, UpdateFundoTipoAtivoLimiteHandler>();
        services.AddScoped<ICommandHandler<TransitionFundoTipoAtivoStatusCommand, RelFundoTipoAtivoDto>, TransitionFundoTipoAtivoStatusHandler>();
        services.AddScoped<IQueryHandler<GetFundoTiposAtivosQuery, PaginatedResult<RelFundoTipoAtivoDto>>, GetFundoTiposAtivosQueryHandler>();

        return services;
    }
}