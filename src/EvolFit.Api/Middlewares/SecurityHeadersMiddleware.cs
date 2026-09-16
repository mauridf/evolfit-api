namespace EvolFit.Api.Middlewares;

/// <summary>
/// Aplica os headers de segurança documentados (SECURITY.md §6).
/// </summary>
public class SecurityHeadersMiddleware
{
    private const string ContentSecurityPolicy = "default-src 'self'";
    private const string PermissionsPolicyValue = "camera=(), microphone=(), geolocation=()";

    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = PermissionsPolicyValue;

        if (!_env.IsDevelopment())
            headers["Content-Security-Policy"] = ContentSecurityPolicy;

        // SECURITY §6 — respostas autenticadas não devem ser cacheadas.
        // OnStarting roda antes do envio dos headers, mesmo p/ respostas
        // que começaram a ser gravadas por middlewares internos.
        context.Response.OnStarting(() =>
        {
            if (context.User.Identity?.IsAuthenticated == true)
                context.Response.Headers["Cache-Control"] = "no-store";
            return Task.CompletedTask;
        });

        await _next(context);
    }
}