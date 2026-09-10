using DbUp;
using Microsoft.Extensions.Configuration;

// ============================================================
// EvolFit.Migrations — runner de migrações SQL via DbUp
// ============================================================
// Uso:
//   dotnet run --project src/EvolFit.Migrations
//
// DbUp:
//   1. Lê a connection string (appsettings.json + env vars).
//   2. Escaneia ./db/migrations/*.sql embutidos como recursos.
//   3. Aplica apenas os scripts que ainda não foram executados.
//   4. Registra cada execução na tabela "schemaversions".
// ============================================================

Console.WriteLine("╔════════════════════════════════════════════════════════╗");
Console.WriteLine("║  EvolFit — Runner de Migrações (DbUp)                  ║");
Console.WriteLine("╚════════════════════════════════════════════════════════╝");

// ------------------------------------------------------------
// 1. Carrega configuração
// ------------------------------------------------------------
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var basePath = ResolveConfigBasePath();

var configuration = new ConfigurationBuilder()
    .SetBasePath(basePath)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' não encontrada. Verifique appsettings.json.");

Console.WriteLine($"→ Ambiente: {environment}");
Console.WriteLine($"→ Aplicando migrações em: {MaskConnectionString(connectionString)}");
Console.WriteLine();

// ------------------------------------------------------------
// 2. Garante que o banco existe (cria se não existir)
// ------------------------------------------------------------
EnsureDatabase.For.PostgresqlDatabase(connectionString);

// ------------------------------------------------------------
// 3. Configura o DbUp para usar scripts SQL embutidos
// ------------------------------------------------------------
var upgrader = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(
        typeof(Program).Assembly,
        // Filtra apenas scripts cujo nome termina com ".sql"
        script => script.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
    .WithTransactionPerScript()   // 1 transação por script (seguro e atômico)
    .LogToConsole()
    .Build();

// ------------------------------------------------------------
// 4. Executa
// ------------------------------------------------------------
var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"✗ Falha na migração: {result.Error}");
    Console.ResetColor();
    return 1;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("✓ Migrações aplicadas com sucesso.");
Console.ResetColor();

foreach (var script in result.Scripts)
{
    Console.WriteLine($"  • {script.Name}");
}

return 0;

// ============================================================
// Helpers locais
// ============================================================
static string ResolveConfigBasePath()
{
    // Prioridade de procura pelo appsettings.json:
    //   1. Diretório que contém o assembly (ex.: quando o projeto roda isolado)
    //   2. CWD → sobe até a pasta do csproj que compartilha o appsettings
    if (File.Exists(Path.Combine(AppContext.BaseDirectory, "appsettings.json")))
        return AppContext.BaseDirectory;

    var current = Directory.GetCurrentDirectory();
    while (current is not null)
    {
        var appSettings = Path.Combine(current, "src", "EvolFit.Api", "appsettings.json");
        if (File.Exists(appSettings))
            return Path.Combine(current, "src", "EvolFit.Api");

        current = Directory.GetParent(current)?.FullName;
    }

    throw new FileNotFoundException(
        "Não foi possível localizar 'src/EvolFit.Api/appsettings.json'. " +
        "Execute o runner a partir da raiz da solution.");
}

static string MaskConnectionString(string cs)
{
    // Mascara a senha ao logar (SEC-005)
    var parts = cs.Split(';', StringSplitOptions.RemoveEmptyEntries);
    var masked = parts.Select(p =>
        p.StartsWith("Password=", StringComparison.OrdinalIgnoreCase)
            ? "Password=***"
            : p);
    return string.Join(";", masked);
}
