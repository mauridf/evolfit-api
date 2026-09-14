using System.Text;
using DbUp;
using EvolFit.Api.Extensions;
using EvolFit.Api.Filters;
using EvolFit.Api.Middlewares;
using EvolFit.Application;
using EvolFit.Infrastructure;
using EvolFit.Infrastructure.Security;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Polly;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando EvolFit API...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console(
            outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.Seq(
            serverUrl: context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341",
            apiKey: context.Configuration["Seq:ApiKey"]));

    // ---------- DI ----------
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddEvolFitRateLimiting(builder.Configuration);

    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, ct) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Cole apenas o token (sem 'Bearer ')."
            };
            return Task.CompletedTask;
        });
    });

    // ---------- JWT ----------
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var secret = jwtSection["Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret não configurado.");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddHealthChecks();

    // SECURITY §5 — HSTS (31536000s = 1 ano, includeSubDomains)
    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromSeconds(31536000);
        options.IncludeSubDomains = true;
    });

    // ---------- CORS ----------
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseHsts();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers()
   .RequireRateLimiting(RateLimitingExtensions.AuthenticatedPolicy);

    // ---------- Health Checks ----------
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => false, // liveness puro
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // ============================================================
    // Migrations no startup (opcional — pode ser desativado por env var)
    // ============================================================
    if (builder.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            logger.LogInformation("Aplicando migrations DbUp...");

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
            var upgrader = DbUp.DeployChanges.To
                .PostgresqlDatabase(connectionString)
.WithScriptsEmbeddedInAssembly(
                    typeof(EvolFit.Migrations.MigrationAssemblyMarker).Assembly,
                    s => s.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                .WithTransactionPerScript()
                .LogToConsole()
                .Build();

            var result = upgrader.PerformUpgrade();
            if (!result.Successful)
            {
                logger.LogError(result.Error, "Falha ao aplicar migrations");
                throw result.Error;
            }
            logger.LogInformation("Migrations aplicadas com sucesso.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Falha crítica ao aplicar migrations.");
            throw;
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EvolFit API falhou ao iniciar");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
