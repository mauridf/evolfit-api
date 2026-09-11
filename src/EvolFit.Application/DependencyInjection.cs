using EvolFit.Application.Features.Auth;
using EvolFit.Application.Features.Health;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EvolFit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Auth
        services.AddScoped<IAuthService, AuthService>();

        // Health
        services.AddScoped<IHealthService, HealthService>();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<AuthService>();

        return services;
    }
}
