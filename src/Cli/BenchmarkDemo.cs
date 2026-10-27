using System.Diagnostics;
using Core;
using Core.Domain;
using Core.Reports;

/// <summary>
/// Додаткове завдання 4: час звітів на 100 000 згенерованих записів (Stopwatch).
/// Для чесного порівняння: однакові дані (фіксований seed), прогрівальний прогін (JIT) і кілька вимірів.
/// Запуск у Release дає реалістичніші числа: dotnet run --project src/Cli -c Release -f net10.0 -- report-bench
/// </summary>
internal static class BenchmarkDemo
{
    public static void Run(int count = 100_000, int threshold = 10, int runs = 10)
    {
        var generation = Stopwatch.StartNew();
        IReadOnlyList<Product> data = SampleData.Generate(count);
        generation.Stop();
        Console.WriteLine($"Згенеровано {data.Count:N0} записів за {generation.Elapsed.TotalMilliseconds:F0} мс; " +
                          $"поріг LowStock < {threshold}; прогонів на варіант: {runs}");
        Console.WriteLine($"  {"Варіант",-50} {"рядків",7} {"середнє, мс",12} {"мін, мс",9}");

        Measure("А. LowStock: один ланцюжок, ToList у кінці", () => ReportVariants.SingleChain(data, threshold).Count, runs);
        Measure("Б. LowStock: ToList після кожного кроку", () => ReportVariants.ToListAfterEachStep(data, threshold).Count, runs);
        Measure("В. LowStock: OrderBy → ToList → Where", () => ReportVariants.SortThenFilter(data, threshold).Count, runs);
        Measure("TopByQuantity(5)", () => ReportService.TopByQuantity(data, 5).Count, runs);
        Measure("ByUnit (GroupBy)", () => ReportService.ByUnit(data).Count, runs);
        Measure("Totals (Sum/Average/Max)", () => ReportService.Totals(data).Items, runs);
    }

    private static void Measure(string title, Func<int> report, int runs)
    {
        int rows = report(); // прогрів: перший виклик включає JIT-компіляцію
        double total = 0, min = double.MaxValue;
        for (int i = 0; i < runs; i++)
        {
            var sw = Stopwatch.StartNew();
            report();
            sw.Stop();
            double ms = sw.Elapsed.TotalMilliseconds;
            total += ms;
            min = Math.Min(min, ms);
        }
        Console.WriteLine($"  {title,-50} {rows,7} {total / runs,12:F2} {min,9:F2}");
    }
}
