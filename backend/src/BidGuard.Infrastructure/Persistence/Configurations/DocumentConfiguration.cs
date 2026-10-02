using BidGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidGuard.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");
        builder.ConfigureBase();

        builder.Property(d => d.OwnerType).IsRequired().AsEnumString();
        builder.Property(d => d.OwnerId).IsRequired();
        builder.Property(d => d.FileName).IsRequired().HasMaxLength(255);
        builder.Property(d => d.StorageKey).IsRequired().HasMaxLength(512);
        builder.HasIndex(d => d.StorageKey).IsUnique();
        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(d => d.SizeBytes).IsRequired();
        builder.Property(d => d.Sha256).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.Property(d => d.DocType).HasMaxLength(64);
        builder.Property(d => d.OcrUsed).IsRequired();
        builder.Property(d => d.ExtractionStatus).IsRequired().AsEnumString();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.OwnerType, d.OwnerId });
    }
}
