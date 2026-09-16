using System.Text.Json;
using EvolFit.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EvolFit.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteProblemDetails(context, StatusCodes.Status422UnprocessableEntity,
                "Validação de negócio", ex.Message, ex.ErrorCode, ex.Errors);
        }
        catch (AppException ex)
        {
            await WriteProblemDetails(context, ex.StatusCode,
                "Application Error", ex.Message, ex.ErrorCode);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblemDetails(context, StatusCodes.Status401Unauthorized,
                "Unauthorized", ex.Message, "UNAUTHORIZED");
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Cliente desistiu/cancelou a requisição (timeout, logout, fechou a página).
            // Nenhuma resposta é gravada — evita 500 espúrio em aborts.
            _logger.LogDebug("Requisição cancelada pelo cliente: {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado");
            await WriteProblemDetails(context, StatusCodes.Status500InternalServerError,
                "Internal Server Error", "Ocorreu um erro inesperado.", "INTERNAL_ERROR");
        }
    }

    private static async Task WriteProblemDetails(
        HttpContext context,
        int status,
        string title,
        string detail,
        string errorCode,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var traceId = context.TraceIdentifier;

        object payload;
        if (errors is not null)
        {
            payload = new
            {
                type = $"https://api.evolfit.app/errors/{errorCode.ToLowerInvariant()}",
                title,
                status,
                detail,
                errorCode,
                errors,
                traceId,
                instance = context.Request.Path.Value
            };
        }
        else
        {
            payload = new
            {
                type = $"https://api.evolfit.app/errors/{errorCode.ToLowerInvariant()}",
                title,
                status,
                detail,
                errorCode,
                traceId,
                instance = context.Request.Path.Value
            };
        }

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
