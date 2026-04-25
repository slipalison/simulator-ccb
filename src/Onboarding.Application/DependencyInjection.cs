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
using Onboarding.Application.Clients.Commands;
using Onboarding.Application.Clients.Validators;
using Onboarding.Application.Common;

namespace Onboarding.Application;

/// <summary>
/// Application layer DI registration — excluded from coverage as it's configuration code.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<
            ICommandHandler<RegisterClientCommand, Guid>,
            RegisterClientCommandHandler>();

        // FluentValidation — manual registration (no auto-pipeline, deprecated in FV 12)
        services.AddScoped<IValidator<RegisterClientCommand>, RegisterClientCommandValidator>();

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

        // Admin queries (Phase 16 — ADMIN-01, ADMIN-02)
        services.AddScoped<IQueryHandler<GetPaginatedUsersQuery, PaginatedResult<UserSummaryDto>>, GetPaginatedUsersHandler>();
        services.AddScoped<IQueryHandler<GetUserDetailsQuery, UserDetailDto>, GetUserDetailsHandler>();

        // Admin commands (Phase 16 — ADMIN-03, ADMIN-04, ADMIN-05)
        services.AddScoped<ICommandHandler<UpdateUserCommand, Unit>, UpdateUserCommandHandler>();
        services.AddScoped<ICommandHandler<BlockUserCommand, Unit>, BlockUserCommandHandler>();
        services.AddScoped<ICommandHandler<UnblockUserCommand, Unit>, UnblockUserCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteUserCommand, Unit>, DeleteUserCommandHandler>();

        // Admin validators
        services.AddScoped<IValidator<UpdateUserCommand>, UpdateUserCommandValidator>();
        services.AddScoped<IValidator<BlockUserCommand>, BlockUserCommandValidator>();
        services.AddScoped<IValidator<UnblockUserCommand>, UnblockUserCommandValidator>();
        services.AddScoped<IValidator<DeleteUserCommand>, DeleteUserCommandValidator>();

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
