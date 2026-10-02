using BidGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidGuard.Infrastructure.Persistence.Configurations;

public sealed class ComplianceRuleConfiguration : IEntityTypeConfiguration<ComplianceRule>
{
    public void Configure(EntityTypeBuilder<ComplianceRule> builder)
    {
        builder.ToTable("compliance_rules");
        builder.ConfigureBase();

        builder.Property(r => r.Code).IsRequired().HasMaxLength(32);
        builder.Property(r => r.Title).IsRequired().HasMaxLength(500);
        builder.Property(r => r.Category).IsRequired().AsEnumString();
        builder.Property(r => r.IsMandatory).IsRequired();
        builder.Property(r => r.Weight).IsRequired().HasPrecision(9, 2);
        builder.Property(r => r.Expression).IsRequired().HasColumnType("jsonb");
        builder.Property(r => r.SourceClause).HasMaxLength(4000);
        builder.Property(r => r.SourcePage);
        builder.Property(r => r.ExemptionKind).AsEnumString();
        builder.Property(r => r.Status).IsRequired().AsEnumString();
        builder.Ignore(r => r.IsExemptionEligible);

        builder.HasIndex(r => new { r.TenderId, r.Code }).IsUnique();
    }
}
