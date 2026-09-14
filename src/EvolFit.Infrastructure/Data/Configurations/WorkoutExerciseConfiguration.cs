using EvolFit.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvolFit.Infrastructure.Data.Configurations;

public class WorkoutExerciseConfiguration : IEntityTypeConfiguration<WorkoutExercise>
{
    public void Configure(EntityTypeBuilder<WorkoutExercise> builder)
    {
        builder.ToTable("workout_exercises");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.WorkoutRoutineId).HasColumnName("workout_routine_id");
        builder.Property(e => e.DayNumber).HasColumnName("day_number");
        builder.Property(e => e.WgerExerciseId).HasColumnName("wger_exercise_id");
        builder.Property(e => e.ExerciseName).HasColumnName("exercise_name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Sets).HasColumnName("sets");
        builder.Property(e => e.Reps).HasColumnName("reps");
        builder.Property(e => e.Weight).HasColumnName("weight").HasPrecision(5, 2);
        builder.Property(e => e.OrderInDay).HasColumnName("order_in_day");

        builder.HasMany(e => e.Logs)
            .WithOne(l => l.WorkoutExercise!)
            .HasForeignKey(l => l.WorkoutExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
