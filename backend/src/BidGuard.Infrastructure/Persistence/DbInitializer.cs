using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BidGuard.Infrastructure.Persistence;

/// <summary>Applies migrations and seeds demo accounts (admin, officer, one bidder organization) when the DB is empty.</summary>
public static class DbInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BidGuardDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await db.Database.MigrateAsync(ct);

        if (await db.Users.AnyAsync(ct))
            return;

        var seed = config.GetSection("Seed");
        var adminPassword = seed["AdminPassword"];
        var officerPassword = seed["OfficerPassword"];
        var bidderPassword = seed["BidderPassword"];
        if (string.IsNullOrWhiteSpace(adminPassword) || string.IsNullOrWhiteSpace(officerPassword) || string.IsNullOrWhiteSpace(bidderPassword))
        {
            logger.LogWarning("Seed passwords not configured; skipping demo account seeding.");
            return;
        }

        var org = new Organization("Shakti Enterprises Pvt Ltd", "AAECS1234K", OrganizationCategory.MSE);
        org.UpdateProfile("Shakti Enterprises Pvt Ltd", OrganizationCategory.MSE, "27AAECS1234K1Z5", "UDYAM-MH-26-0012345",
            "U72900MH2019PTC123456", "MHBAN0012345000", isStartup: false, dpiitNumber: null);

        db.Organizations.Add(org);
        db.Users.AddRange(
            new User("admin@bidguard.local", hasher.Hash(adminPassword), "Platform Admin", UserRole.Admin),
            new User("officer@bidguard.local", hasher.Hash(officerPassword), "Procurement Officer", UserRole.Officer),
            new User("bidder@shakti.local", hasher.Hash(bidderPassword), "Shakti Bid Manager", UserRole.Bidder, org.Id));

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded demo admin, officer and bidder accounts.");
    }
}
