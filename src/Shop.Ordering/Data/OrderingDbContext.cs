using Microsoft.EntityFrameworkCore;
using Shop.Ordering.Orders;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.DistributedEvents;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Shop.Ordering.Data;

[ConnectionStringName("Ordering")]
public class OrderingDbContext(DbContextOptions<OrderingDbContext> options)
    : AbpDbContext<OrderingDbContext>(options), IHasEventOutbox, IHasEventInbox
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutgoingEventRecord> OutgoingEvents { get; set; } = null!;
    public DbSet<IncomingEventRecord> IncomingEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ConfigureEventOutbox();
        builder.ConfigureEventInbox();

        builder.Entity<Order>(b =>
        {
            b.ToTable("Orders");
            b.ConfigureByConvention();
            b.Property(o => o.Total).HasPrecision(18, 2);
            b.Property(o => o.Status).HasConversion<string>().HasMaxLength(16);
            b.Property(o => o.RejectionReason).HasMaxLength(512);
            b.HasMany(o => o.Lines).WithOne().HasForeignKey("OrderId").IsRequired();
            b.Navigation(o => o.Lines).AutoInclude();
        });

        builder.Entity<OrderLine>(b =>
        {
            b.ToTable("OrderLines");
            b.ConfigureByConvention();
            b.Property(l => l.UnitPrice).HasPrecision(18, 2);
        });
    }
}
