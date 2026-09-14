using EvolFit.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvolFit.Infrastructure.Data.Configurations;

public class ExerciseLogConfiguration : IEntityTypeConfiguration<ExerciseLog>
{
    public void Configure(EntityTypeBuilder<ExerciseLog> builder)
    {
        builder.ToTable("exercise_logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.UserId).HasColumnName("user_id");
        builder.Property(l => l.WorkoutExerciseId).HasColumnName("workout_exercise_id");
        builder.Property(l => l.Date).HasColumnName("date");
        builder.Property(l => l.Completed).HasColumnName("completed");
    }
}
