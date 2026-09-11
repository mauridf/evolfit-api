using EvolFit.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvolFit.Infrastructure.Data.Configurations;

public class WgerExerciseCacheConfiguration : IEntityTypeConfiguration<WgerExerciseCache>
{
    public void Configure(EntityTypeBuilder<WgerExerciseCache> builder)
    {
        builder.ToTable("wger_exercises_cache");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.WgerExerciseId).HasColumnName("wger_exercise_id");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.Category).HasColumnName("category").HasMaxLength(100);
        builder.Property(e => e.MusclesJson).HasColumnName("muscles").HasColumnType("jsonb");
        builder.Property(e => e.EquipmentJson).HasColumnName("equipment").HasColumnType("jsonb");
        builder.Property(e => e.ImagesJson).HasColumnName("images").HasColumnType("jsonb");
        builder.Property(e => e.CachedAt).HasColumnName("cached_at");
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at");

        builder.HasIndex(e => e.WgerExerciseId).IsUnique();
    }
}
