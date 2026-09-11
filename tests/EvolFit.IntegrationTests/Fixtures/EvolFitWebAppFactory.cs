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

        builder.ConfigureServices(services =>
        {
            // Remove DbContext registrado e substitui pela connection do Testcontainer
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<EvolFitDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<EvolFitDbContext>(options =>
                options.UseNpgsql(_connectionString));

            // Cria o schema
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EvolFitDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
