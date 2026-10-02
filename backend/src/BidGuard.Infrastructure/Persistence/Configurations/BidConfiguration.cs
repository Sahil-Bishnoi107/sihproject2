using BidGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidGuard.Infrastructure.Persistence.Configurations;

public sealed class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable("bids");
        builder.ConfigureBase();

        builder.Property(b => b.IsSimulation).IsRequired();
        builder.Property(b => b.Status).IsRequired().AsEnumString();
        builder.Property(b => b.SubmittedAt);
        builder.Property(b => b.OverallScore).HasPrecision(5, 2);
        builder.Property(b => b.SuggestedDecision).AsEnumString();
        builder.Property(b => b.Decision).AsEnumString();
        builder.Property(b => b.DecisionRemarks).HasMaxLength(2000);
        builder.Property(b => b.DecidedAt);
        builder.Property(b => b.FailureReason).HasMaxLength(2000);
        builder.Ignore(b => b.CanAcceptDocuments);

        builder.HasOne(b => b.Tender)
            .WithMany()
            .HasForeignKey(b => b.TenderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.Organization)
            .WithMany()
            .HasForeignKey(b => b.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(b => b.DecidedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.TenderId, b.IsSimulation });
        builder.HasIndex(b => b.OrganizationId);
    }
}
