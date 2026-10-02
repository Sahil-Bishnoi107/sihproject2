using BidGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidGuard.Infrastructure.Persistence.Configurations;

public sealed class ExtractedFieldConfiguration : IEntityTypeConfiguration<ExtractedField>
{
    public void Configure(EntityTypeBuilder<ExtractedField> builder)
    {
        builder.ToTable("extracted_fields");
        builder.ConfigureBase();

        builder.Property(f => f.Key).IsRequired().HasMaxLength(100);
        builder.Property(f => f.Value).IsRequired().HasMaxLength(2000);
        builder.Property(f => f.Confidence).IsRequired().HasPrecision(4, 3);
        builder.Property(f => f.Page);
        builder.Property(f => f.BoundingBox).HasColumnType("jsonb");

        builder.HasOne<Bid>()
            .WithMany()
            .HasForeignKey(f => f.BidId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(f => f.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new { f.BidId, f.Key });
    }
}
