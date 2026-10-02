using BidGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidGuard.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.ConfigureBase();

        builder.Property(n => n.Type).IsRequired().AsEnumString();
        builder.Property(n => n.Message).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.ReferenceId).HasMaxLength(100);
        builder.Property(n => n.IsRead).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.UserId, n.IsRead });
    }
}
