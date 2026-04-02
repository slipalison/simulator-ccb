using Microsoft.Extensions.DependencyInjection;
using Onboarding.Application.Clients.Commands;
using Onboarding.Application.Common;

namespace Onboarding.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<
            ICommandHandler<RegisterClientCommand, Guid>,
            RegisterClientCommandHandler>();
        return services;
    }
}
