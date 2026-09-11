using EvolFit.Application.Common;
using EvolFit.Application.Features.Auth;
using EvolFit.Application.Features.Auth.Interfaces;
using EvolFit.Application.Features.Health.Interfaces;
using EvolFit.Application.Features.TinyFn.Interfaces;
using EvolFit.Application.Features.Wger.Interfaces;
using EvolFit.Application.Features.Workouts.Interfaces;
using EvolFit.Infrastructure.Caching;
using EvolFit.Infrastructure.Data;
using EvolFit.Infrastructure.Data.Context;
using EvolFit.Infrastructure.Data.Repositories;
using EvolFit.Infrastructure.ExternalServices;
using EvolFit.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EvolFit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<EvolFitDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // JWT
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<AuthOptions>(options =>
        {
            options.RefreshTokenExpireDays =
                int.TryParse(configuration["Jwt:RefreshTokenExpireDays"], out var days) ? days : 7;
        });

        // Repositórios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IHealthMetricRepository, HealthMetricRepository>();
        services.AddScoped<IWorkoutRoutineRepository, WorkoutRoutineRepository>();
        services.AddScoped<IWorkoutExerciseRepository, WorkoutExerciseRepository>();
        services.AddScoped<IExerciseLogRepository, ExerciseLogRepository>();
        services.AddScoped<IWgerExerciseCacheRepository, WgerExerciseCacheRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Segurança
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        // Current user
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Cache
        services.AddMemoryCache();
        services.AddSingleton<ITinyFnCache, MemoryTinyFnCache>();

        // HTTP clients externos (TinyFn + Polly)
        services.AddExternalHttpClients(configuration);

        return services;
    }
}
