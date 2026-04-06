using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
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

        return services;
    }
}
