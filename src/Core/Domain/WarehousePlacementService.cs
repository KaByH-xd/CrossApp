namespace Core.Domain;

/// <summary>
/// Правило, що охоплює ДВІ сутності: «на складі не більше N різних товарів, а один товар
/// зберігається лише на одному складі». Жодна з сутностей не знає про інші екземпляри,
/// тому правило живе в окремому сервісі (на п'ятому тижні його викличе CatalogService).
/// </summary>
public sealed class WarehousePlacementService
{
    private readonly Dictionary<string, HashSet<string>> _productsByWarehouse = new();
    private readonly Dictionary<string, string> _warehouseByProduct = new();

    public int MaxProductsPerWarehouse { get; }

    public WarehousePlacementService(int maxProductsPerWarehouse = 5)
    {
        if (maxProductsPerWarehouse < 1)
            throw new ArgumentOutOfRangeException(nameof(maxProductsPerWarehouse), maxProductsPerWarehouse,
                "Максимум товарів на складі має бути не менше 1");
        MaxProductsPerWarehouse = maxProductsPerWarehouse;
    }

    public void Place(Warehouse warehouse, Product product)
    {
        ArgumentNullException.ThrowIfNull(warehouse);
        ArgumentNullException.ThrowIfNull(product);

        if (_warehouseByProduct.TryGetValue(product.Id, out string? current))
            throw new InvalidOperationException(
                $"Товар {product.Id} уже розміщений на складі {current}");

        int count = _productsByWarehouse.TryGetValue(warehouse.Id, out HashSet<string>? set) ? set.Count : 0;
        if (count >= MaxProductsPerWarehouse)
            throw new InvalidOperationException(
                $"Склад {warehouse.Id} заповнений: уже {count} із {MaxProductsPerWarehouse} товарів, " +
                $"товар {product.Id} додати не можна");

        if (set is null)
        {
            set = new HashSet<string>();
            _productsByWarehouse[warehouse.Id] = set;
        }
        set.Add(product.Id);
        _warehouseByProduct[product.Id] = warehouse.Id;
    }

    // Копія, а не внутрішня колекція: зовнішній код не може обійти перевірки.
    public IReadOnlyList<string> GetProductIds(Warehouse warehouse)
    {
        ArgumentNullException.ThrowIfNull(warehouse);
        return _productsByWarehouse.TryGetValue(warehouse.Id, out HashSet<string>? set)
            ? set.ToArray()
            : Array.Empty<string>();
    }
}
