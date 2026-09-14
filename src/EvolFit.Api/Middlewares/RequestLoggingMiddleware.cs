using System.Diagnostics;
using System.Security.Claims;
using Serilog.Context;

namespace EvolFit.Api.Middlewares;

/// <summary>
/// Enriquece os logs do Serilog com TraceId e UserId (quando autenticado).
/// Deve ficar ANTES do UseAuthentication para poder logar requisições anônimas,
/// e ser reaplicado depois para enriquecer com UserId — ou apenas enriquecer
/// no final do pipeline.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("RequestPath", path))
        using (LogContext.PushProperty("RequestMethod", method))
        {
            context.Response.Headers["X-Trace-Id"] = traceId;

            await _next(context);
        }
    }
}
