using BidGuard.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidGuard.Infrastructure.Persistence.Configurations;

internal static class EntityConfigurationExtensions
{
    public const int EnumLength = 40;

    /// <summary>Key and timestamps shared by every entity. Ids are UUIDv7 generated in the domain.</summary>
    public static void ConfigureBase<T>(this EntityTypeBuilder<T> builder) where T : BaseEntity
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
    }

    /// <summary>Stores an enum as its name so the database stays readable and reorder-safe.</summary>
    public static PropertyBuilder<TEnum> AsEnumString<TEnum>(this PropertyBuilder<TEnum> property) =>
        property.HasConversion<string>().HasMaxLength(EnumLength);
}
