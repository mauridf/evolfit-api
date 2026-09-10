using Serilog;

// ============================================================
// Bootstrap do Serilog (antes do host iniciar)
// ============================================================
// Log.Logger é temporário — será substituído pelo logger
// configurado a partir do appsettings.json quando o host iniciar.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando EvolFit API...");

    var builder = WebApplication.CreateBuilder(args);

    // ========================================================
    // Serilog: lê configuração de appsettings.json + enrichers
    // ========================================================
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

    // ========================================================
    // Controllers
    // ========================================================
    builder.Services.AddControllers();

    // ========================================================
    // OpenAPI nativo do .NET 10 (usado apenas para gerar o
    // documento consumido pelo Scalar — não expõe Swagger UI)
    // ========================================================
    builder.Services.AddOpenApi();

    // ========================================================
    // Health Checks
    // ========================================================
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // ========================================================
    // Pipeline HTTP
    // ========================================================
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        // Documento OpenAPI em /openapi/v1.json
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();

    app.MapControllers();

    // Endpoint simples de liveness (readiness virá no Bloco 7)
    app.MapHealthChecks("/health");

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
