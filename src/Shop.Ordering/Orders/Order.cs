using System.Text.Json.Serialization;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;

namespace Shop.Ordering.Orders;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    /// <summary>Waiting for Catalog to reserve stock.</summary>
    Pending,
    Confirmed,
    Rejected,
}

public class Order : FullAuditedAggregateRoot<Guid>
{
    public OrderStatus Status { get; private set; }
    public decimal Total { get; private set; }
    public string? RejectionReason { get; private set; }
    public List<OrderLine> Lines { get; private set; } = [];

    private Order() { } // EF Core

    public Order(Guid id, IEnumerable<(Guid ProductId, int Quantity)> lines) : base(id)
    {
        foreach (var (productId, quantity) in lines)
        {
            if (quantity <= 0) throw new BusinessException(OrderingErrorCodes.InvalidQuantity);
            Lines.Add(new OrderLine(productId, quantity));
        }
        if (Lines.Count == 0) throw new BusinessException(OrderingErrorCodes.EmptyOrder);
        Status = OrderStatus.Pending;
    }

    /// <summary>Stock was reserved; lock in the prices Catalog reserved at.</summary>
    public void Confirm(IReadOnlyDictionary<Guid, decimal> unitPrices)
    {
        EnsurePending();
        foreach (var line in Lines)
        {
            if (!unitPrices.TryGetValue(line.ProductId, out var price))
                throw new BusinessException(OrderingErrorCodes.MissingPrice).WithData("productId", line.ProductId);
            line.SetUnitPrice(price);
        }
        Total = Lines.Sum(l => l.UnitPrice!.Value * l.Quantity);
        Status = OrderStatus.Confirmed;
    }

    public void Reject(string reason)
    {
        EnsurePending();
        RejectionReason = Check.NotNullOrWhiteSpace(reason, nameof(reason), maxLength: 512);
        Status = OrderStatus.Rejected;
    }

    private void EnsurePending()
    {
        if (Status != OrderStatus.Pending)
            throw new BusinessException(OrderingErrorCodes.NotPending).WithData("status", Status);
    }
}

public class OrderLine : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal? UnitPrice { get; private set; }

    private OrderLine() { } // EF Core

    internal OrderLine(Guid productId, int quantity) : base(Guid.NewGuid())
    {
        ProductId = productId;
        Quantity = quantity;
    }

    internal void SetUnitPrice(decimal price) => UnitPrice = price;
}

public static class OrderingErrorCodes
{
    public const string EmptyOrder = "Ordering:EmptyOrder";
    public const string InvalidQuantity = "Ordering:InvalidQuantity";
    public const string NotPending = "Ordering:NotPending";
    public const string MissingPrice = "Ordering:MissingPrice";
}
