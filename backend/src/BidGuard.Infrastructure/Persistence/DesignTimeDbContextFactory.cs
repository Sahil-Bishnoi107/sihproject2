using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BidGuard.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef` build the model without booting the API host.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BidGuardDbContext>
{
    public BidGuardDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("BIDGUARD_DESIGN_DB")
                               ?? "Host=localhost;Port=5433;Database=bidguard;Username=bidguard;Password=bidguard_dev";
        var options = new DbContextOptionsBuilder<BidGuardDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new BidGuardDbContext(options);
    }
}
