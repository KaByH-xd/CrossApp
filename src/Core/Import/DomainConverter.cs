using Core.Domain;
using Core.Dto;

namespace Core.Import;

/// <summary>
/// Перетворює результат імпорту (DTO) на сутності. Усе, що не пройшло інваріанти домену,
/// потрапляє в Errors — та сама ідея «дані + помилки», що й у ImportResult.
/// </summary>
public static class DomainConverter
{
    public static DomainImportResult ToDomain(MultiImportResult source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var products = new List<Product>();
        var warehouses = new List<Warehouse>();
        var errors = new List<string>();
        var productIds = new HashSet<string>();
        var warehouseIds = new HashSet<string>();

        foreach (ProductDto dto in source.Products)
        {
            try
            {
                Product product = Product.FromDto(dto);
                if (!productIds.Add(product.Id))
                {
                    errors.Add($"Товар {product.Id}: дублікат ідентифікатора");
                    continue;
                }
                products.Add(product);
            }
            catch (ArgumentException ex)
            {
                errors.Add($"Товар '{dto.Id}': {FirstLine(ex.Message)}");
            }
            catch (InvalidOperationException ex)
            {
                errors.Add($"Товар '{dto.Id}': {FirstLine(ex.Message)}");
            }
        }

        foreach (WarehouseDto dto in source.Warehouses)
        {
            try
            {
                Warehouse warehouse = Warehouse.FromDto(dto);
                if (!warehouseIds.Add(warehouse.Id))
                {
                    errors.Add($"Склад {warehouse.Id}: дублікат ідентифікатора");
                    continue;
                }
                warehouses.Add(warehouse);
            }
            catch (ArgumentException ex)
            {
                errors.Add($"Склад '{dto.Id}': {FirstLine(ex.Message)}");
            }
        }

        return new DomainImportResult(products, warehouses, errors);
    }

    // Повідомлення ArgumentException містить другим рядком "(Parameter ...)"; для звіту беремо перший.
    private static string FirstLine(string message)
    {
        int index = message.IndexOfAny(new[] { '\r', '\n' });
        return index < 0 ? message : message[..index];
    }
}
