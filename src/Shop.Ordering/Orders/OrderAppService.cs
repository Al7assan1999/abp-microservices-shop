using System.ComponentModel.DataAnnotations;
using Shop.Contracts;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;

namespace Shop.Ordering.Orders;

public record OrderLineDto(Guid ProductId, int Quantity, decimal? UnitPrice);

public record OrderDto(Guid Id, OrderStatus Status, decimal Total, string? RejectionReason, DateTime CreationTime, List<OrderLineDto> Lines);

public class CreateOrderLineDto
{
    public Guid ProductId { get; set; }
    [Range(1, 1000)] public int Quantity { get; set; }
}

public class CreateOrderDto
{
    [Required, MinLength(1)] public List<CreateOrderLineDto> Lines { get; set; } = [];
}

/// <summary>Exposed as /api/app/order by ABP's auto API controllers.</summary>
public class OrderAppService(IRepository<Order, Guid> orders, IDistributedEventBus eventBus) : ApplicationService
{
    /// <summary>
    /// Saves the order as Pending and asks Catalog to reserve stock. The event goes through the outbox,
    /// so it is published if and only if the order is committed.
    /// </summary>
    public async Task<OrderDto> CreateAsync(CreateOrderDto input)
    {
        var order = new Order(GuidGenerator.Create(), input.Lines.Select(l => (l.ProductId, l.Quantity)));
        await orders.InsertAsync(order);

        await eventBus.PublishAsync(new OrderPlacedEto(
            order.Id,
            order.Lines.Select(l => new OrderLineEto(l.ProductId, l.Quantity)).ToList()));

        return ToDto(order);
    }

    public async Task<OrderDto> GetAsync(Guid id) => ToDto(await orders.GetAsync(id));

    public async Task<ListResultDto<OrderDto>> GetListAsync()
    {
        var query = (await orders.WithDetailsAsync(o => o.Lines)).OrderByDescending(o => o.CreationTime).Take(50);
        return new ListResultDto<OrderDto>((await AsyncExecuter.ToListAsync(query)).Select(ToDto).ToList());
    }

    private static OrderDto ToDto(Order o) => new(
        o.Id, o.Status, o.Total, o.RejectionReason, o.CreationTime,
        o.Lines.Select(l => new OrderLineDto(l.ProductId, l.Quantity, l.UnitPrice)).ToList());
}
