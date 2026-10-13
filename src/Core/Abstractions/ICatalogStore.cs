using Core.Domain;

namespace Core.Abstractions;

/// <summary>
/// Контракт сховища каталогу товарів. Лише операції, потрібні сервісу:
/// нічого про файли, бази даних чи кеш тут немає.
/// </summary>
public interface ICatalogStore
{
    /// <summary>Усі товари; колекція лише для читання, зміни йдуть через Add/Update/Remove.</summary>
    IReadOnlyList<Product> List();

    /// <summary>Товар за id або null, якщо запису немає (це нормальний результат пошуку).</summary>
    Product? GetById(string id);

    /// <summary>Додає новий запис; якщо id уже зайнятий — InvalidOperationException.</summary>
    void Add(Product item);

    /// <summary>Зберігає зміни існуючого запису; якщо запису немає — InvalidOperationException.</summary>
    void Update(Product item);

    /// <summary>Видаляє запис; false, якщо такого id не було.</summary>
    bool Remove(string id);
}
