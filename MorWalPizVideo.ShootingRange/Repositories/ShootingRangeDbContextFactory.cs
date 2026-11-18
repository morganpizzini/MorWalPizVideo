using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MorWalPizVideo.ShootingRange.Repositories;

public sealed class ShootingRangeDbContextFactory : IDesignTimeDbContextFactory<ShootingRangeDbContext>
{
    public ShootingRangeDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ShootingRangeDatabase__ConnectionString");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Set ShootingRangeDatabase__ConnectionString for EF tooling.");

        var options = new DbContextOptionsBuilder<ShootingRangeDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new ShootingRangeDbContext(options);
    }
}
