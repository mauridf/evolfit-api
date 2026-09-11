using DbUp;
using EvolFit.Infrastructure.Data.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EvolFit.IntegrationTests.Fixtures;

public class EvolFitWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public EvolFitWebAppFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Segredos não vêm mais dos appsettings (SEC-002) — os testes fornecem os seus.
        builder.UseSetting("Jwt:Secret",
            "test_secret_evolfit_01234567890abcdef01234567890abcdef");

        builder.ConfigureServices(services =>
        {
            // Remove DbContext registrado e substitui pela connection do Testcontainer
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<EvolFitDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<EvolFitDbContext>(options =>
                options.UseNpgsql(_connectionString));

            // Cria o schema com as migrations reais (DbUp), não com EnsureCreated
            var result = DeployChanges.To
                .PostgresqlDatabase(_connectionString)
                .WithScriptsEmbeddedInAssembly(
                    typeof(EvolFit.Migrations.MigrationAssemblyMarker).Assembly,
                    s => s.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                .WithTransactionPerScript()
                .Build()
                .PerformUpgrade();

            if (!result.Successful)
                throw new InvalidOperationException("Falha ao aplicar migrations DbUp", result.Error);
        });
    }
}