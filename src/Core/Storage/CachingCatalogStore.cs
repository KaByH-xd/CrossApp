using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

/// <summary>
/// Декоратор: той самий контракт ICatalogStore, нова поведінка — запам'ятовує результати читання
/// і скидає кеш після будь-якої зміни. Обгортає ІНШЕ сховище, отримане через конструктор.
/// </summary>
public sealed class CachingCatalogStore(ICatalogStore inner) : ICatalogStore
{
    private readonly ICatalogStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly Dictionary<string, Product> _byId = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<Product>? _all;

    public int Hits { get; private set; }
    public int Misses { get; private set; }

    public IReadOnlyList<Product> List()
    {
        if (_all is not null)
        {
            Hits++;
            return _all;
        }
        Misses++;
        _all = _inner.List();
        return _all;
    }

    public Product? GetById(string id)
    {
        if (_byId.TryGetValue(id, out Product? cached))
        {
            Hits++;
            return cached;
        }
        Misses++;
        Product? found = _inner.GetById(id);
        if (found is not null)
            _byId[id] = found;
        return found;
    }

    public void Add(Product item)
    {
        _inner.Add(item);
        Invalidate();
    }

    public void Update(Product item)
    {
        _inner.Update(item);
        Invalidate();
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        Invalidate();
        return removed;
    }

    private void Invalidate()
    {
        _all = null;
        _byId.Clear();
    }
}
