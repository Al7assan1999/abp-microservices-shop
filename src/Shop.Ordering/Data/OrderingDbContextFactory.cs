using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shop.Ordering.Data;

/// <summary>Lets `dotnet ef` create the DbContext without booting the whole ABP application.</summary>
public class OrderingDbContextFactory : IDesignTimeDbContextFactory<OrderingDbContext>
{
    public OrderingDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseNpgsql(configuration.GetConnectionString("Ordering"));

        return new OrderingDbContext(options.Options);
    }
}
