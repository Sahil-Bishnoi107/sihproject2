using BidGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidGuard.Infrastructure.Persistence.Configurations;

public sealed class TenderConfiguration : IEntityTypeConfiguration<Tender>
{
    public void Configure(EntityTypeBuilder<Tender> builder)
    {
        builder.ToTable("tenders");
        builder.ConfigureBase();

        builder.Property(t => t.GemBidNumber).IsRequired().HasMaxLength(64);
        builder.HasIndex(t => t.GemBidNumber).IsUnique();
        builder.Property(t => t.Title).IsRequired().HasMaxLength(500);
        builder.Property(t => t.Category).IsRequired().HasMaxLength(200);
        builder.Property(t => t.EstimatedValue).IsRequired().HasPrecision(18, 2);
        builder.Property(t => t.EmdAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(t => t.BidEndDate).IsRequired();
        builder.Property(t => t.Status).IsRequired().AsEnumString();
        builder.Property(t => t.FailureReason).HasMaxLength(1000);
        builder.Property(t => t.PublishedAt);
        builder.Ignore(t => t.RulesEditable);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(t => t.TenderDocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(t => t.Rules)
            .WithOne()
            .HasForeignKey(r => r.TenderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.Rules).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(t => t.Status);
    }
}
