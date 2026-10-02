using BidGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidGuard.Infrastructure.Persistence.Configurations;

public sealed class VerificationResultConfiguration : IEntityTypeConfiguration<VerificationResult>
{
    public void Configure(EntityTypeBuilder<VerificationResult> builder)
    {
        builder.ToTable("verification_results");
        builder.ConfigureBase();

        builder.Property(v => v.Source).IsRequired().AsEnumString();
        builder.Property(v => v.Identifier).IsRequired().HasMaxLength(64);
        builder.Property(v => v.Status).IsRequired().AsEnumString();
        builder.Property(v => v.RegisteredName).HasMaxLength(300);
        builder.Property(v => v.Details).HasColumnType("jsonb");
        builder.Property(v => v.CheckedAt).IsRequired();

        builder.HasOne<Bid>()
            .WithMany()
            .HasForeignKey(v => v.BidId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => new { v.BidId, v.Source });
    }
}
