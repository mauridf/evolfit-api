using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;

namespace EvolFit.Api.Extensions;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";
    public const string AuthenticatedPolicy = "authenticated";

    public static IServiceCollection AddEvolFitRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Limites configuráveis (RateLimiting:Auth:PermitLimit, RateLimiting:Authenticated:PermitLimit)
        var authPermitLimit = configuration.GetValue<int?>("RateLimiting:Auth:PermitLimit") ?? 5;
        var authenticatedPermitLimit =
            configuration.GetValue<int?>("RateLimiting:Authenticated:PermitLimit") ?? 100;

        services.AddRateLimiter(options =>
        {
            // Resposta 429 em vez do 503 padrão
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // ---------- Política: Auth (5 req/min por IP) ----------
            // Aplicada em /auth/login, /auth/register, /auth/refresh
            options.AddPolicy(AuthPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    }));

            // ---------- Política: Authenticated (100 req/min por user) ----------
            // Aplicada globalmente a endpoints autenticados
            options.AddPolicy(AuthenticatedPolicy, httpContext =>
            {
                var userId = httpContext.User?.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authenticatedPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
            });

            // Quando rejeitar, envia Retry-After (SEC-004)
            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(
                        MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }

                context.HttpContext.Response.ContentType = "application/problem+json";

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://api.evolfit.app/errors/rate-limit",
                    title = "Too Many Requests",
                    status = 429,
                    detail = "Você excedeu o limite de requisições. Tente novamente em alguns instantes.",
                    traceId = context.HttpContext.TraceIdentifier
                }, cancellationToken: ct);
            };
        });

        return services;
    }
}
