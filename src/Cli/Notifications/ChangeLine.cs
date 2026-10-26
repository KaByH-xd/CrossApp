using System.Globalization;
using Core.Events;

/// <summary>
/// Один формат рядка журналу для всіх одержувачів у Cli (обробник події, ConsoleSink, FileSink),
/// щоб консоль і файл не розходилися. Формат — справа Cli, не Core.
/// </summary>
internal static class ChangeLine
{
    public static string Format(CatalogChangedEventArgs e)
    {
        string line = $"{e.At:HH:mm:ss} {e.Kind,-8} {e.Id} {e.Delta,4:+0;-0} → залишок {e.QuantityAfter}";
        return e.Kind == CatalogChangeKind.Repriced
            ? $"{line}, нова ціна {e.PriceAfter.ToString("F2", CultureInfo.InvariantCulture)}"
            : line;
    }
}
