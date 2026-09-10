using EvolFit.Application.Features.Auth;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EvolFit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Auth
        services.AddScoped<IAuthService, AuthService>();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<AuthService>();

        return services;
    }
}
