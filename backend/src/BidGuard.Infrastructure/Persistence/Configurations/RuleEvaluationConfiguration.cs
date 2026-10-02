using BidGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidGuard.Infrastructure.Persistence.Configurations;

public sealed class RuleEvaluationConfiguration : IEntityTypeConfiguration<RuleEvaluation>
{
    public void Configure(EntityTypeBuilder<RuleEvaluation> builder)
    {
        builder.ToTable("rule_evaluations");
        builder.ConfigureBase();

        builder.Property(e => e.Outcome).IsRequired().AsEnumString();
        builder.Property(e => e.Explanation).IsRequired().HasMaxLength(4000);
        builder.Property(e => e.EvidenceJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.OfficerOverride).AsEnumString();
        builder.Property(e => e.OverrideReason).HasMaxLength(1000);
        builder.Property(e => e.ReviewedAt);
        builder.Ignore(e => e.EffectiveOutcome);

        builder.HasOne<Bid>()
            .WithMany()
            .HasForeignKey(e => e.BidId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Rule)
            .WithMany()
            .HasForeignKey(e => e.RuleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.BidId, e.RuleId }).IsUnique();
    }
}
