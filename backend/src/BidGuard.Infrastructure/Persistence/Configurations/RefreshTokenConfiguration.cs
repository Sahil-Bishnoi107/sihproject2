using BidGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidGuard.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.ConfigureBase();

        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(128);
        builder.Property(t => t.CreatedByIp).HasMaxLength(64);
        builder.Ignore(t => t.IsRevoked);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(t => new { t.UserId, t.RevokedAt });
    }
}
