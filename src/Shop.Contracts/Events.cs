using Volo.Abp.EventBus;

namespace Shop.Contracts;

// Integration events exchanged over RabbitMQ. This assembly is the only thing the services share;
// neither service references the other's code or database.

public record OrderLineEto(Guid ProductId, int Quantity);

/// <summary>Ordering → Catalog: please reserve stock for these lines.</summary>
[EventName("Shop.OrderPlaced")]
public record OrderPlacedEto(Guid OrderId, List<OrderLineEto> Lines);

public record ReservedLineEto(Guid ProductId, int Quantity, decimal UnitPrice);

/// <summary>Catalog → Ordering: stock is reserved, here are the prices it was reserved at.</summary>
[EventName("Shop.StockReserved")]
public record StockReservedEto(Guid OrderId, List<ReservedLineEto> Lines);

/// <summary>Catalog → Ordering: the order can't be fulfilled; nothing was reserved.</summary>
[EventName("Shop.StockReservationFailed")]
public record StockReservationFailedEto(Guid OrderId, string Reason);
