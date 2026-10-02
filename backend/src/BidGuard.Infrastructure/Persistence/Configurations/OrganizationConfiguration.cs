using BidGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidGuard.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.ConfigureBase();

        builder.Property(o => o.LegalName).IsRequired().HasMaxLength(300);
        builder.Property(o => o.Pan).IsRequired().HasMaxLength(10).IsFixedLength();
        builder.HasIndex(o => o.Pan).IsUnique();
        builder.Property(o => o.Gstin).HasMaxLength(15).IsFixedLength();
        builder.Property(o => o.UdyamNumber).HasMaxLength(32);
        builder.Property(o => o.Cin).HasMaxLength(21).IsFixedLength();
        builder.Property(o => o.EpfoCode).HasMaxLength(32);
        builder.Property(o => o.Category).IsRequired().AsEnumString();
        builder.Property(o => o.IsStartup).IsRequired();
        builder.Property(o => o.DpiitNumber).HasMaxLength(32);
        builder.Ignore(o => o.IsRecognisedStartup);

        builder.HasMany(o => o.Credentials)
            .WithOne()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.Credentials).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
