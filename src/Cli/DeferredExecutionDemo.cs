using Core.Domain;
using Core.Services;

/// <summary>
/// Крок 3: демонстрація відкладеного виконання LINQ. Це навчальне демо самого LINQ, а не звіт,
/// тому Where тут живе в окремому файлі Cli, а Program.cs лишається без жодного Where.
/// </summary>
internal static class DeferredExecutionDemo
{
    public static void Run(CatalogService service)
    {
        // service.All() щоразу повертає новий знімок, тому джерелом беремо власний локальний список.
        List<Product> source = service.All().ToList();

        int checks = 0; // замикання: скільки разів лямбда-умова реально викликалась
        IEnumerable<Product> query = source.Where(p =>
        {
            checks++;
            return p.Quantity < 10;
        });
        Console.WriteLine($"Запит побудовано; умову перевірено разів: {checks} (запит ще не виконувався)");

        Console.WriteLine($"Дефіцитних до змін: {query.Count()}   (перевірок: {checks})");

        source.Add(Product.Create("TMP-0001", "Скоба монтажна", 0.50m, 3, unit: "уп"));
        Console.WriteLine($"Дефіцитних після змін: {query.Count()}   (перевірок: {checks}) — запит виконано ще раз, " +
                          "новий елемент потрапив у результат");

        List<Product> snapshot = query.ToList(); // результат зафіксовано
        source.Clear();
        Console.WriteLine($"Запит після source.Clear(): {query.Count()}");
        Console.WriteLine($"У знімку лишилось: {snapshot.Count}   — список після ToList від джерела не залежить");
    }
}
