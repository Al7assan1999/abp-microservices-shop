using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shop.Catalog.Data;

/// <summary>Lets `dotnet ef` create the DbContext without booting the whole ABP application.</summary>
public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(configuration.GetConnectionString("Catalog"));

        return new CatalogDbContext(options.Options);
    }
}
