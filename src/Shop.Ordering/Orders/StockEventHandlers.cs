using Shop.Contracts;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;

namespace Shop.Ordering.Orders;

/// <summary>Completes the order once Catalog has answered. Both handlers run through the inbox.</summary>
public class StockEventHandlers(IRepository<Order, Guid> orders) :
    IDistributedEventHandler<StockReservedEto>,
    IDistributedEventHandler<StockReservationFailedEto>,
    ITransientDependency
{
    public async Task HandleEventAsync(StockReservedEto e)
    {
        var order = await orders.GetAsync(e.OrderId);
        order.Confirm(e.Lines.ToDictionary(l => l.ProductId, l => l.UnitPrice));
        await orders.UpdateAsync(order);
    }

    public async Task HandleEventAsync(StockReservationFailedEto e)
    {
        var order = await orders.GetAsync(e.OrderId);
        order.Reject(e.Reason);
        await orders.UpdateAsync(order);
    }
}
