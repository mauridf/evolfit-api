using EvolFit.Application.Features.Auth;
using EvolFit.Application.Features.Health;
using EvolFit.Application.Features.Workouts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EvolFit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<IWorkoutService, WorkoutService>();
        services.AddScoped<IWorkoutGeneratorService, WorkoutGeneratorService>();

        services.AddValidatorsFromAssemblyContaining<AuthService>();
        return services;
    }
}
