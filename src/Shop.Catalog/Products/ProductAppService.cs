using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Shop.Catalog.Products;

public record ProductDto(Guid Id, string Name, decimal Price, int StockCount);

public class CreateProductDto
{
    [Required, StringLength(ProductConsts.MaxNameLength)] public string Name { get; set; } = "";
    [Range(0.01, 1_000_000)] public decimal Price { get; set; }
    [Range(0, 1_000_000)] public int StockCount { get; set; }
}

public class AddStockDto
{
    [Range(1, 1_000_000)] public int Quantity { get; set; }
}

/// <summary>Exposed as /api/app/product by ABP's auto API controllers.</summary>
public class ProductAppService(IRepository<Product, Guid> products) : ApplicationService
{
    public async Task<ProductDto> GetAsync(Guid id) => ToDto(await products.GetAsync(id));

    public async Task<ListResultDto<ProductDto>> GetListAsync()
    {
        var list = await products.GetListAsync();
        return new ListResultDto<ProductDto>(list.OrderBy(p => p.Name).Select(ToDto).ToList());
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto input)
    {
        var product = new Product(GuidGenerator.Create(), input.Name, input.Price, input.StockCount);
        return ToDto(await products.InsertAsync(product));
    }

    public async Task<ProductDto> AddStockAsync(Guid id, AddStockDto input)
    {
        var product = await products.GetAsync(id);
        product.AddStock(input.Quantity);
        return ToDto(await products.UpdateAsync(product));
    }

    private static ProductDto ToDto(Product p) => new(p.Id, p.Name, p.Price, p.StockCount);
}
