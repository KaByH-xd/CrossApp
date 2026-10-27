using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Reports;
using Core.Services;
using Core.Storage;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;


// ═════════════ Composition root: єдине місце з конкретними класами сховищ ═════════════
bool useFile = args.Contains("--file");
bool empty = args.Contains("--empty"); // порожнє джерело: перевірка, що звіти не падають
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

ICatalogStore store;
if (args.Contains("--factory"))
{
    store = StoreFactory.Create(args, dataPath); // той самий вибір, але через фабрику
}
else
{
    store = useFile
        ? new FileCatalogStore(dataPath)
        : new InMemoryCatalogStore(empty ? Array.Empty<Product>() : SampleData.Products());
    if (args.Contains("--cache"))
        store = new CachingCatalogStore(store); // декоратор обгортає вибране сховище
}

var service = new CatalogService(store);
// ═══════════════════════════════════════════════════════════════════════════════════════

// Перший аргумент без "--" — назва підкоманди; прапорці --file/--cache/--empty назвою звіту не стають.
string command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "report-top";
IReadOnlyList<Product> items = service.All(); // один знімок для всіх звітів цього запуску

Console.WriteLine($"Сховище: {store.GetType().Name}, записів: {items.Count}");
if (useFile)
    Console.WriteLine($"Файл даних: {Path.GetFullPath(dataPath)}");

// У Program.cs лише вибір звіту і вивід; фільтри, групування й агрегати — у Core.Reports.ReportService.
switch (command)
{
    case "report-top":
        ReportPrinter.Title("Топ-5 за залишком");
        ReportPrinter.Print(ReportService.TopByQuantity(items, 5));
        break;
    case "report-low":
        ReportPrinter.Title("Дефіцит: залишок < 10");
        ReportPrinter.Print(ReportService.LowStock(items, 10));
        break;
    case "report-groups":
        ReportPrinter.Title("Групування за одиницею виміру");
        ReportPrinter.Print(ReportService.ByUnit(items));
        break;
    case "report-sum":
        ReportPrinter.Title("Підсумки");
        ReportPrinter.Print(ReportService.Totals(items));
        break;
    case "report-value":
        ReportPrinter.Title("Топ-5 за вартістю залишку (ціна × кількість)");
        ReportPrinter.Print(ReportService.TopByValue(items, 5));
        break;
    case "report-all":
        ReportPrinter.Title("Топ-5 за залишком");
        ReportPrinter.Print(ReportService.TopByQuantity(items, 5));
        ReportPrinter.Title("Дефіцит: залишок < 10");
        ReportPrinter.Print(ReportService.LowStock(items, 10));
        ReportPrinter.Title("Групування за одиницею виміру");
        ReportPrinter.Print(ReportService.ByUnit(items));
        ReportPrinter.Title("Підсумки");
        ReportPrinter.Print(ReportService.Totals(items));
        ReportPrinter.Title("Топ-5 за вартістю залишку (ціна × кількість)");
        ReportPrinter.Print(ReportService.TopByValue(items, 5));
        break;
    case "report-groups-query":
        IReadOnlyList<GroupSummary> methodSyntax = ReportService.ByUnit(items);
        IReadOnlyList<GroupSummary> querySyntax = ReportService.ByUnitQuerySyntax(items);
        ReportPrinter.Title("Групування: синтаксис методів (ланцюжок викликів)");
        ReportPrinter.Print(methodSyntax);
        ReportPrinter.Title("Групування: синтаксис запитів (from … group … into …)");
        ReportPrinter.Print(querySyntax);
        // record порівнюються за значенням, тож SequenceEqual перевіряє вміст, а не посилання
        Console.WriteLine($"  Результати однакові: {methodSyntax.SequenceEqual(querySyntax)}");
        break;
    case "report-deferred":
        ReportPrinter.Title("Відкладене виконання LINQ");
        DeferredExecutionDemo.Run(service);
        break;
    case "report-moves":
        ReportPrinter.Title("Рух товарів (з подій Changed)");
        MovementsDemo.Run(service);
        break;
    case "report-bench":
        ReportPrinter.Title("Вимірювання Stopwatch на 100 000 записів");
        BenchmarkDemo.Run();
        break;
    case "lab6":
        EventsDemo.Run(service, store, args);
        break;
    default:
        Console.WriteLine($"Невідома команда «{command}». Доступні:");
        Console.WriteLine("  report-top | report-low | report-groups | report-sum | report-value | report-all");
        Console.WriteLine("  report-groups-query | report-deferred | report-moves | report-bench | lab6 [файл]");
        Console.WriteLine("  прапорці: --file | --cache | --factory | --empty");
        return 1;
}

if (store is CachingCatalogStore cache && command != "lab6")
    Console.WriteLine($"Кеш: влучань {cache.Hits}, промахів {cache.Misses}");

return 0;
