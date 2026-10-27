using Core.Domain;

namespace Core.Reports;

/// <summary>
/// Додаткове завдання 4: варіанти одного й того самого звіту LowStock для вимірювання Stopwatch.
/// Результат у всіх однаковий, різниться лише місце матеріалізації (ToList) у ланцюжку.
/// </summary>
public static class ReportVariants
{
    // А. Один ланцюжок, ToList лише в кінці (так, як у ReportService.LowStock).
    public static IReadOnlyList<StockLine> SingleChain(IEnumerable<Product> source, int threshold) =>
        ReportService.LowStock(source, threshold);

    // Б. ToList після кожного кроку: ті самі операції + проміжні списки (зайві алокації й копіювання).
    public static IReadOnlyList<StockLine> ToListAfterEachStep(IEnumerable<Product> source, int threshold)
    {
        List<Product> filtered = source.Where(p => p.Quantity < threshold).ToList();
        List<Product> sorted = filtered.OrderBy(p => p.Quantity).ThenBy(p => p.Id, StringComparer.Ordinal).ToList();
        return sorted.Select(p => new StockLine(p.Id, p.Name, p.Quantity, p.Unit)).ToList();
    }

    // В. Сортування до фільтра і ToList посередині: сортуються ВСІ записи, а потім більшість відкидається
    //    (типова помилка «Where після OrderBy»).
    public static IReadOnlyList<StockLine> SortThenFilter(IEnumerable<Product> source, int threshold) =>
        source.OrderBy(p => p.Quantity).ThenBy(p => p.Id, StringComparer.Ordinal)
            .ToList()
            .Where(p => p.Quantity < threshold)
            .Select(p => new StockLine(p.Id, p.Name, p.Quantity, p.Unit))
            .ToList();
}
