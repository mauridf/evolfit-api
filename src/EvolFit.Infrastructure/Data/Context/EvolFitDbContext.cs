using EvolFit.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvolFit.Infrastructure.Data.Context;

public class EvolFitDbContext : DbContext
{
    public EvolFitDbContext(DbContextOptions<EvolFitDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<HealthMetric> HealthMetrics => Set<HealthMetric>();
    public DbSet<WorkoutRoutine> WorkoutRoutines => Set<WorkoutRoutine>();
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();
    public DbSet<ExerciseLog> ExerciseLogs => Set<ExerciseLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EvolFitDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
