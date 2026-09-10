using EvolFit.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvolFit.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.UserId).HasColumnName("user_id");
        builder.Property(t => t.TokenHash).HasColumnName("token_hash").HasMaxLength(500).IsRequired();
        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.RevokedAt).HasColumnName("revoked_at");
        builder.Property(t => t.ReplacedById).HasColumnName("replaced_by_id");

        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.HasOne(t => t.ReplacedBy)
            .WithMany()
            .HasForeignKey(t => t.ReplacedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
