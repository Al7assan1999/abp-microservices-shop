using Microsoft.EntityFrameworkCore;
using Shop.Catalog.Products;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.DistributedEvents;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Shop.Catalog.Data;

[ConnectionStringName("Catalog")]
public class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : AbpDbContext<CatalogDbContext>(options), IHasEventOutbox, IHasEventInbox
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<OutgoingEventRecord> OutgoingEvents { get; set; } = null!;
    public DbSet<IncomingEventRecord> IncomingEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ConfigureEventOutbox();
        builder.ConfigureEventInbox();

        builder.Entity<Product>(b =>
        {
            b.ToTable("Products");
            b.ConfigureByConvention();
            b.Property(p => p.Name).IsRequired().HasMaxLength(ProductConsts.MaxNameLength);
            b.Property(p => p.Price).HasPrecision(18, 2);
        });
    }
}
