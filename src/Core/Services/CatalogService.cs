using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

/// <summary>
/// Бізнес-операції над каталогом. Залежить лише від контракту ICatalogStore:
/// чим саме є сховище, сервіс не знає. Інваріанти перевіряє сутність Product.
/// Про успішні зміни сервіс повідомляє подією <see cref="Changed"/> (і, за потреби, через
/// <see cref="INotificationSink"/>), але сам нічого не виводить і нікуди не пише.
/// </summary>
public sealed class CatalogService(ICatalogStore store, params INotificationSink[] sinks)
{
    private readonly ICatalogStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink[] _sinks = sinks ?? [];

    /// <summary>
    /// Сповіщення «у каталозі щось змінилось». Піднімається рівно один раз на кожну успішну операцію
    /// і лише ПІСЛЯ зміни стану; операція, що впала на інваріанті, сповіщення не дає.
    /// </summary>
    public event EventHandler<CatalogChangedEventArgs>? Changed;

    // Єдине місце підняття події: ?. — підписників може не бути, це нормальний стан сервісу.
    private void OnChanged(Product p, CatalogChangeKind kind, int delta)
    {
        var args = new CatalogChangedEventArgs(p.Id, kind, delta, p.Quantity, p.Price);
        Changed?.Invoke(this, args);
        foreach (INotificationSink sink in _sinks)
            sink.Notify(args);
    }

    public Product Add(string name, decimal price, int quantity, string? note = null,
        string unit = Product.DefaultUnit)
    {
        Product product = Product.Create(Guid.NewGuid().ToString("N")[..8], name, price, quantity, note, unit);
        _store.Add(product); // інваріанти вже перевірив Product.Create; сховище перевіряє лише унікальність id
        OnChanged(product, CatalogChangeKind.Added, quantity); // після зміни, не до
        return product;
    }

    public void Receive(string id, int quantity)
    {
        Product product = GetOrThrow(id);
        product.RegisterArrival(quantity); // якщо інваріант кине виняток — події не буде
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Received, quantity);
    }

    public void Issue(string id, int quantity)
    {
        Product product = GetOrThrow(id);
        product.Issue(quantity);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Issued, -quantity); // видача зменшує залишок
    }

    public void ChangePrice(string id, decimal newPrice)
    {
        Product product = GetOrThrow(id);
        product.ChangePrice(newPrice);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Repriced, 0); // залишок не змінився
    }

    public void Remove(string id)
    {
        Product product = GetOrThrow(id);
        if (product.Quantity > 0)
            throw new InvalidOperationException(
                $"Не можна видалити товар {product.Id}: залишок {product.Quantity} має бути нульовим");
        if (!_store.Remove(product.Id))
            return; // запис уже зник зі сховища — зміни не сталося, сповіщати нема про що
        OnChanged(product, CatalogChangeKind.Removed, 0);
    }

    public IReadOnlyList<Product> All() => _store.List();

    public Product? Find(string id) => _store.GetById(id);

    // Поруч із Find(string id): тут критерій задає той, хто викликає. Потрібне значення у відповідь,
    // тому це делегат Func, а не подія.
    public IReadOnlyList<Product> FindAll(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    // Назва з лабораторної 5; залишена для сумісності, працює через FindAll.
    public IReadOnlyList<Product> Search(Func<Product, bool> predicate) => FindAll(predicate);

    private Product GetOrThrow(string id) =>
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає запису з id={id}.");
}
