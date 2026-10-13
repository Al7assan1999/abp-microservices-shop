using Shop.Contracts;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;

namespace Shop.Catalog.Products;

/// <summary>
/// Reserves stock for a placed order, all lines or nothing, and replies with the outcome.
/// Runs through the inbox (duplicate deliveries are ignored) and publishes through the outbox,
/// so the stock change and the reply are committed in the same database transaction.
/// </summary>
public class OrderPlacedHandler(IRepository<Product, Guid> products, IDistributedEventBus eventBus)
    : IDistributedEventHandler<OrderPlacedEto>, ITransientDependency
{
    public async Task HandleEventAsync(OrderPlacedEto order)
    {
        var ids = order.Lines.Select(l => l.ProductId).Distinct().ToList();
        var found = (await products.GetListAsync(p => ids.Contains(p.Id))).ToDictionary(p => p.Id);

        // Merge duplicate lines for the same product before checking stock.
        var requested = order.Lines
            .GroupBy(l => l.ProductId)
            .Select(g => (ProductId: g.Key, Quantity: g.Sum(l => l.Quantity)))
            .ToList();

        var problem = requested
            .Select(r => !found.TryGetValue(r.ProductId, out var p)
                ? $"Product {r.ProductId} does not exist"
                : !p.CanReserve(r.Quantity) ? $"Only {p.StockCount} of '{p.Name}' in stock" : null)
            .FirstOrDefault(m => m is not null);

        if (problem is not null)
        {
            await eventBus.PublishAsync(new StockReservationFailedEto(order.OrderId, problem));
            return;
        }

        var reserved = new List<ReservedLineEto>();
        foreach (var (productId, quantity) in requested)
        {
            var product = found[productId];
            product.Reserve(quantity);
            await products.UpdateAsync(product);
            reserved.Add(new ReservedLineEto(productId, quantity, product.Price));
        }

        await eventBus.PublishAsync(new StockReservedEto(order.OrderId, reserved));
    }
}
