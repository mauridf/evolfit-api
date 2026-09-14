using EvolFit.Core.Entities;
using EvolFit.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvolFit.Infrastructure.Data.Configurations;

public class HealthMetricConfiguration : IEntityTypeConfiguration<HealthMetric>
{
    public void Configure(EntityTypeBuilder<HealthMetric> builder)
    {
        builder.ToTable("health_metrics");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.UserId).HasColumnName("user_id");
        builder.Property(m => m.HeightCm).HasColumnName("height_cm").HasPrecision(5, 2);
        builder.Property(m => m.WeightKg).HasColumnName("weight_kg").HasPrecision(5, 2);
        builder.Property(m => m.Bmi).HasColumnName("bmi").HasPrecision(4, 2);
        builder.Property(m => m.Bmr).HasColumnName("bmr");
        builder.Property(m => m.Tdee).HasColumnName("tdee");
        builder.Property(m => m.ActivityLevel)
            .HasColumnName("activity_level")
            .HasMaxLength(20)
            .HasConversion(
                v => v.HasValue ? v.Value.ToApiValue() : null,
                v => string.IsNullOrEmpty(v) ? null : EvolFit.Core.Enums.ActivityLevelExtensions.FromApiValue(v));
        builder.Property(m => m.MeasuredAt).HasColumnName("measured_at");
    }
}
