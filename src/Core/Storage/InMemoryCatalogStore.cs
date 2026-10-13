using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

/// <summary>Сховище в оперативній пам'яті: після завершення програми дані зникають.</summary>
public sealed class InMemoryCatalogStore(IEnumerable<Product>? seed = null) : ICatalogStore
{
    // Дублікат id у seed одразу кине виняток із ToDictionary — ще під час створення сховища.
    private readonly Dictionary<string, Product> _items =
        (seed ?? []).ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Product> List() => _items.Values.ToList().AsReadOnly();

    public Product? GetById(string id) => _items.GetValueOrDefault(id);

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_items.ContainsKey(item.Id))
            throw new InvalidOperationException($"Запис з id={item.Id} уже існує.");
        _items.Add(item.Id, item);
    }

    public void Update(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.ContainsKey(item.Id))
            throw new InvalidOperationException($"Немає запису з id={item.Id}.");
        _items[item.Id] = item;
    }

    public bool Remove(string id) => _items.Remove(id);
}
