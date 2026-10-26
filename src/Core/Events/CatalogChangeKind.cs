namespace Core.Events;

/// <summary>
/// Вид зміни каталогу, про яку сповіщає <see cref="Services.CatalogService.Changed"/>.
/// Подія виникає лише після успішної операції: якщо інваріант кинув виняток, сповіщення немає.
/// </summary>
public enum CatalogChangeKind
{
    /// <summary>Новий товар додано до каталогу (Delta = початковий залишок).</summary>
    Added,

    /// <summary>Прихід: залишок збільшився (Delta &gt; 0).</summary>
    Received,

    /// <summary>Видача: залишок зменшився (Delta &lt; 0).</summary>
    Issued,

    /// <summary>Товар видалено з каталогу (залишок на момент видалення нульовий, Delta = 0).</summary>
    Removed,

    /// <summary>Змінено ціну; залишок не змінюється (Delta = 0), нова ціна — у PriceAfter.</summary>
    Repriced
}
