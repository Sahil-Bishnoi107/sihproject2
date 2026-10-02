using BidGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidGuard.Infrastructure.Persistence.Configurations;

public sealed class CredentialConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.ToTable("credentials");
        builder.ConfigureBase();

        builder.Property(c => c.Type).IsRequired().AsEnumString();
        builder.Property(c => c.Number).IsRequired().HasMaxLength(64);
        builder.Property(c => c.IssuedOn);
        builder.Property(c => c.ExpiresOn);
        builder.Property(c => c.LastExpiryAlertAt);

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => new { c.OrganizationId, c.Type });
        builder.HasIndex(c => c.ExpiresOn);
    }
}
