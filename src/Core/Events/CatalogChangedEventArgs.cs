namespace Core.Events;

/// <summary>
/// Дані події «каталог змінився». Знімок стану на момент сповіщення: властивості лише для читання,
/// тож підписник не може змінити те, що побачать наступні обробники.
/// </summary>
public sealed class CatalogChangedEventArgs(
    string id, CatalogChangeKind kind, int delta, int quantityAfter, decimal priceAfter) : EventArgs
{
    /// <summary>Ідентифікатор товару, якого стосується зміна.</summary>
    public string Id { get; } = id;

    /// <summary>Що саме сталося.</summary>
    public CatalogChangeKind Kind { get; } = kind;

    /// <summary>На скільки змінилась кількість: «+» прихід/додавання, «−» видача, 0 — без зміни залишку.</summary>
    public int Delta { get; } = delta;

    /// <summary>Залишок після зміни.</summary>
    public int QuantityAfter { get; } = quantityAfter;

    /// <summary>Ціна після зміни (для Repriced — нова ціна).</summary>
    public decimal PriceAfter { get; } = priceAfter;

    /// <summary>Момент створення аргументів події.</summary>
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}
