namespace EvolFit.Migrations;

// Marcador público usado por outros projetos (ex.: EvolFit.Api)
// para obter o assembly do runner de migrações e seus scripts SQL
// embutidos via DbUp sem depender do "Program" gerado por top-level statements.
public static class MigrationAssemblyMarker
{
    public static System.Reflection.Assembly Assembly =>
        System.Reflection.Assembly.GetExecutingAssembly();
}