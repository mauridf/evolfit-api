using EvolFit.Application.Common;
using EvolFit.Application.Features.Auth;
using EvolFit.Application.Features.Auth.Interfaces;
using EvolFit.Infrastructure.Data;
using EvolFit.Infrastructure.Data.Context;
using EvolFit.Infrastructure.Data.Repositories;
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
            options.ExpireMinutes =
                int.TryParse(configuration["Jwt:ExpireMinutes"], out var minutes) ? minutes : 120;
        });

        // Repositórios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Segurança
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        // Current user (usa IHttpContextAccessor)
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
