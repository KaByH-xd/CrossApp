using Core.Domain;

namespace Core.Reports;

/// <summary>
/// Звіти через LINQ to Objects. Кожен метод — чиста функція: приймає IEnumerable (а не сховище),
/// тому його можна викликати з будь-якого джерела і легко перевірити тестом. Повертає IReadOnlyList DTO:
/// результат уже матеріалізований (ToList), повторне читання Count чи foreach у Cli запит не перезапускає.
///
/// Коли виконується запит: ланцюжок Where/OrderBy/Select/GroupBy лише описує обчислення (відкладене
/// виконання); реальний прохід по джерелу стається в завершальному ToList() усередині кожного методу.
/// Тобто кожен звіт читає джерело рівно один раз — у момент виклику методу, а не в момент виводу в Cli.
/// </summary>
public static class ReportService
{
    // 1) Фільтр + сортування + проєкція: «яких товарів менше за поріг?»
    //    Where стоїть ПЕРШИМ — сортуються лише відібрані елементи, а не всі.
    public static IReadOnlyList<StockLine> LowStock(IEnumerable<Product> source, int threshold) =>
        source.Where(p => p.Quantity < threshold)
            .OrderBy(p => p.Quantity).ThenBy(p => p.Id, StringComparer.Ordinal)
            .Select(ToStockLine)
            .ToList();

    // 2) Топ-N зі стабільним порядком: «яких товарів найбільше?» (другий ключ — унікальний Id).
    public static IReadOnlyList<StockLine> TopByQuantity(IEnumerable<Product> source, int n) =>
        source.TopBy(p => p.Quantity, n, p => p.Id)
            .Select(ToStockLine)
            .ToList();

    // 3) Групування з підрахунком і сумою: «як залишки розподіляються за одиницями виміру?»
    //    OrdinalIgnoreCase: "уп" і "УП" — одна група; g.Key — значення ключа першого елемента групи.
    public static IReadOnlyList<GroupSummary> ByUnit(IEnumerable<Product> source) =>
        source.GroupBy(p => p.Unit, StringComparer.OrdinalIgnoreCase)
            .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)))
            .OrderByDescending(s => s.TotalQuantity).ThenBy(s => s.Key, StringComparer.Ordinal)
            .ToList();

    // 3б) Додаткове завдання 3: той самий звіт у синтаксисі запитів.
    //     У «group ... by» компаратор передати не можна, тому ключ нормалізується явно (ToLowerInvariant);
    //     let дає ім'я сумі, щоб не рахувати її двічі (в orderby і в select).
    public static IReadOnlyList<GroupSummary> ByUnitQuerySyntax(IEnumerable<Product> source) =>
        (from product in source
         group product by product.Unit.ToLowerInvariant() into g
         let total = g.Sum(p => p.Quantity)
         orderby total descending, g.Key
         select new GroupSummary(g.Key, g.Count(), total))
        .ToList();

    // 4) Агрегати з обробкою порожньої послідовності: «скільки всього, яке середнє, який максимум?»
    //    Average і Max на порожній послідовності кидають InvalidOperationException, тому спершу — перевірка.
    public static (int Items, int Total, double Average, int Max) Totals(IEnumerable<Product> source)
    {
        var list = source.ToList(); // одна матеріалізація: далі чотири агрегати по готовому списку
        return list.Count == 0
            ? (0, 0, 0d, 0)
            : (list.Count, list.Sum(p => p.Quantity), list.Average(p => p.Quantity), list.Max(p => p.Quantity));
    }

    // 5) Топ-N за вартістю залишку (Price × Quantity) — друге використання розширення TopBy.
    public static IReadOnlyList<StockValueLine> TopByValue(IEnumerable<Product> source, int n) =>
        source.TopBy(p => p.Price * p.Quantity, n, p => p.Id)
            .Select(p => new StockValueLine(p.Id, p.Name, p.Quantity, p.Unit, p.Price, p.Price * p.Quantity))
            .ToList();

    // 6) Додаткове завдання 1, Join: журнал руху з назвами товарів.
    //    Внутрішнє з'єднання: рух товару, якого вже немає в каталозі (видалений), у результат не потрапляє.
    //    Зовнішня послідовність — movements, тому порядок результату — хронологічний порядок руху.
    public static IReadOnlyList<MovementLine> MovementJournal(
        IEnumerable<Product> products, IEnumerable<StockMovement> movements) =>
        movements.Join(products,
                movement => movement.ProductId,
                product => product.Id,
                (movement, product) => new MovementLine(product.Id, product.Name, movement.Kind, movement.Delta, product.Unit))
            .ToList();

    // 7) Додаткове завдання 1, GroupJoin: зведення руху по КОЖНОМУ товару, навіть без переміщень (Moves = 0).
    public static IReadOnlyList<MovementSummary> MovementsByProduct(
        IEnumerable<Product> products, IEnumerable<StockMovement> movements) =>
        products.GroupJoin(movements,
                product => product.Id,
                movement => movement.ProductId,
                (product, moves) => new MovementSummary(
                    product.Id,
                    product.Name,
                    moves.Count(),
                    moves.Where(m => m.Delta > 0).Sum(m => m.Delta),
                    -moves.Where(m => m.Delta < 0).Sum(m => m.Delta),
                    product.Quantity,
                    product.Unit))
            .OrderByDescending(s => s.Moves).ThenBy(s => s.Id, StringComparer.Ordinal)
            .ToList();

    private static StockLine ToStockLine(Product p) => new(p.Id, p.Name, p.Quantity, p.Unit);
}
