using BidGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidGuard.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.ConfigureBase();

        builder.Property(a => a.Sequence).IsRequired();
        builder.HasIndex(a => a.Sequence).IsUnique();
        builder.Property(a => a.Timestamp).IsRequired();
        builder.Property(a => a.ActorId);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityId).IsRequired().HasMaxLength(100);
        // text, not jsonb: jsonb re-serialises the payload, which would change the bytes the hash covers.
        builder.Property(a => a.PayloadJson).IsRequired().HasColumnType("text");
        builder.Property(a => a.PrevHash).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.Property(a => a.Hash).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.HasIndex(a => a.Hash).IsUnique();

        builder.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}
