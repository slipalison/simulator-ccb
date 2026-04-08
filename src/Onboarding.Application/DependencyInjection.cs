using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Application.Auth.Commands;
using Onboarding.Application.Auth.DTOs;
using Onboarding.Application.Auth.Validators;
using Onboarding.Application.Clients.Commands;
using Onboarding.Application.Clients.Validators;
using Onboarding.Application.Common;

namespace Onboarding.Application;

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

        return services;
    }
}
