using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Shop.Catalog.Products;

public class Product : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    public int StockCount { get; private set; }

    private Product() { } // EF Core

    public Product(Guid id, string name, decimal price, int stockCount) : base(id)
    {
        Rename(name);
        SetPrice(price);
        AddStock(stockCount);
    }

    public void Rename(string name) =>
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: ProductConsts.MaxNameLength);

    public void SetPrice(decimal price)
    {
        if (price <= 0) throw new BusinessException(CatalogErrorCodes.InvalidPrice);
        Price = price;
    }

    public void AddStock(int quantity)
    {
        if (quantity < 0) throw new BusinessException(CatalogErrorCodes.InvalidQuantity);
        StockCount += quantity;
    }

    public bool CanReserve(int quantity) => quantity > 0 && quantity <= StockCount;

    public void Reserve(int quantity)
    {
        if (!CanReserve(quantity))
            throw new BusinessException(CatalogErrorCodes.InsufficientStock).WithData("productId", Id);
        StockCount -= quantity;
    }
}

public static class ProductConsts
{
    public const int MaxNameLength = 128;
}

public static class CatalogErrorCodes
{
    public const string InvalidPrice = "Catalog:InvalidPrice";
    public const string InvalidQuantity = "Catalog:InvalidQuantity";
    public const string InsufficientStock = "Catalog:InsufficientStock";
}
