using Shop.Catalog.Products;
using Shop.Ordering.Orders;
using Volo.Abp;

namespace Shop.Tests;

public class ProductTests
{
    private static Product NewProduct(int stock = 10) => new(Guid.NewGuid(), "Keyboard", 49.90m, stock);

    [Fact]
    public void Reserve_decrements_stock()
    {
        var product = NewProduct(10);

        product.Reserve(3);

        Assert.Equal(7, product.StockCount);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Cannot_reserve_more_than_stock_or_non_positive(int quantity)
    {
        var product = NewProduct(10);

        Assert.False(product.CanReserve(quantity));
        var e = Assert.Throws<BusinessException>(() => product.Reserve(quantity));
        Assert.Equal(CatalogErrorCodes.InsufficientStock, e.Code);
        Assert.Equal(10, product.StockCount);
    }

    [Fact]
    public void Price_must_be_positive()
    {
        var e = Assert.Throws<BusinessException>(() => new Product(Guid.NewGuid(), "Free thing", 0m, 1));
        Assert.Equal(CatalogErrorCodes.InvalidPrice, e.Code);
    }
}

public class OrderTests
{
    private static readonly Guid Keyboard = Guid.NewGuid();
    private static readonly Guid Mouse = Guid.NewGuid();

    private static Order NewOrder() => new(Guid.NewGuid(), [(Keyboard, 2), (Mouse, 1)]);

    [Fact]
    public void New_order_is_pending()
    {
        var order = NewOrder();

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(0m, order.Total);
    }

    [Fact]
    public void Confirm_uses_reserved_prices_for_the_total()
    {
        var order = NewOrder();

        order.Confirm(new Dictionary<Guid, decimal> { [Keyboard] = 50m, [Mouse] = 20m });

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(120m, order.Total);
    }

    [Fact]
    public void Reject_records_reason()
    {
        var order = NewOrder();

        order.Reject("Only 1 of 'Keyboard' in stock");

        Assert.Equal(OrderStatus.Rejected, order.Status);
        Assert.Equal("Only 1 of 'Keyboard' in stock", order.RejectionReason);
    }

    [Fact]
    public void A_decided_order_cannot_change_again()
    {
        var order = NewOrder();
        order.Reject("out of stock");

        var e = Assert.Throws<BusinessException>(() => order.Confirm(new Dictionary<Guid, decimal> { [Keyboard] = 1m, [Mouse] = 1m }));
        Assert.Equal(OrderingErrorCodes.NotPending, e.Code);
    }

    [Fact]
    public void Confirm_requires_a_price_for_every_line()
    {
        var order = NewOrder();

        var e = Assert.Throws<BusinessException>(() => order.Confirm(new Dictionary<Guid, decimal> { [Keyboard] = 50m }));
        Assert.Equal(OrderingErrorCodes.MissingPrice, e.Code);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void Empty_or_invalid_orders_are_refused()
    {
        Assert.Equal(OrderingErrorCodes.EmptyOrder, Assert.Throws<BusinessException>(() => new Order(Guid.NewGuid(), [])).Code);
        Assert.Equal(OrderingErrorCodes.InvalidQuantity, Assert.Throws<BusinessException>(() => new Order(Guid.NewGuid(), [(Keyboard, 0)])).Code);
    }
}
