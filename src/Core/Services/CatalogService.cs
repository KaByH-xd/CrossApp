using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

/// <summary>
/// Бізнес-операції над каталогом. Залежить лише від контракту ICatalogStore:
/// чим саме є сховище, сервіс не знає. Інваріанти перевіряє сутність Product.
/// </summary>
public sealed class CatalogService(ICatalogStore store)
{
    private readonly ICatalogStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public Product Add(string name, decimal price, int quantity, string? note = null)
    {
        Product product = Product.Create(Guid.NewGuid().ToString("N")[..8], name, price, quantity, note);
        _store.Add(product); // інваріанти вже перевірив Product.Create; сховище перевіряє лише унікальність id
        return product;
    }

    public void Receive(string id, int quantity)
    {
        Product product = GetOrThrow(id);
        product.RegisterArrival(quantity); // метод сутності з тижня 4
        _store.Update(product);
    }

    public void Issue(string id, int quantity)
    {
        Product product = GetOrThrow(id);
        product.Issue(quantity);
        _store.Update(product);
    }

    public void ChangePrice(string id, decimal newPrice)
    {
        Product product = GetOrThrow(id);
        product.ChangePrice(newPrice);
        _store.Update(product);
    }

    public void Remove(string id)
    {
        Product product = GetOrThrow(id);
        if (product.Quantity > 0)
            throw new InvalidOperationException(
                $"Не можна видалити товар {product.Id}: залишок {product.Quantity} має бути нульовим");
        _store.Remove(product.Id);
    }

    public IReadOnlyList<Product> All() => _store.List();

    public Product? Find(string id) => _store.GetById(id);

    // Пошук за довільною умовою; підготовка до LINQ-звітів (тижні 6–7).
    public IReadOnlyList<Product> Search(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Array.AsReadOnly(_store.List().Where(predicate).ToArray());
    }

    private Product GetOrThrow(string id) =>
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає запису з id={id}.");
}
