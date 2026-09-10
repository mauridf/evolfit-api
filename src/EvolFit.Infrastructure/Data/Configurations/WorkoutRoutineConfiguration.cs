using EvolFit.Core.Entities;
using EvolFit.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvolFit.Infrastructure.Data.Configurations;

public class WorkoutRoutineConfiguration : IEntityTypeConfiguration<WorkoutRoutine>
{
    public void Configure(EntityTypeBuilder<WorkoutRoutine> builder)
    {
        builder.ToTable("workout_routines");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.UserId).HasColumnName("user_id");
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(r => r.Goal)
            .HasColumnName("goal")
            .HasMaxLength(50)
            .HasConversion(
                v => v.HasValue ? v.Value.ToApiValue() : null,
                v => string.IsNullOrEmpty(v) ? null : EvolFit.Core.Enums.GoalExtensions.FromApiValue(v));
        builder.Property(r => r.StartDate).HasColumnName("start_date");
        builder.Property(r => r.EndDate).HasColumnName("end_date");
        builder.Property(r => r.Status).HasColumnName("status");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        builder.HasMany(r => r.Exercises)
            .WithOne(e => e.WorkoutRoutine!)
            .HasForeignKey(e => e.WorkoutRoutineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
