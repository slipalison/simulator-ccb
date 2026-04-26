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
        services.AddScoped<ICommandHandler<RegisterEmployeeCommand, RegisterEmployeeResult>, RegisterEmployeeCommandHandler>();
        services.AddScoped<IValidator<RegisterEmployeeCommand>, RegisterEmployeeCommandValidator>();

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

        // Admin commands — Company/Employee (Phase 37 — D-19)
        services.AddScoped<ICommandHandler<UpdateCompanyCommand, Unit>, UpdateCompanyCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteEmployeeCommand, Unit>, DeleteEmployeeCommandHandler>();
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

        return services;
    }
}