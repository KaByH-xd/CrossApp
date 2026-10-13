using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;


// ═════════════ Composition root: єдине місце з конкретними класами сховищ ═════════════
bool useFile = args.Contains("--file");
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
        : new InMemoryCatalogStore(SampleData.Products());
    if (args.Contains("--cache"))
        store = new CachingCatalogStore(store); // декоратор обгортає вибране сховище
}

var service = new CatalogService(store);
// ═══════════════════════════════════════════════════════════════════════════════════════

Console.WriteLine($"Сховище: {store.GetType().Name}");
if (useFile)
    Console.WriteLine($"Файл даних: {Path.GetFullPath(dataPath)}");
Console.WriteLine($"Записів на старті: {service.All().Count}");

// ───────────── Сценарій 1: успіх ─────────────
Console.WriteLine();
Console.WriteLine("=== Сценарій 1: додати, змінити, знайти, показати ===");
Product created = service.Add("Кабель UTP cat6", 28.50m, 50, "бухта");
Console.WriteLine($"Додано:  {created}");
service.Receive(created.Id, 25);
service.Issue(created.Id, 10);
service.ChangePrice(created.Id, 31.00m);
Console.WriteLine($"Знайдено за id: {service.Find(created.Id)}");

Console.WriteLine($"Усього записів: {service.All().Count}");
foreach (Product p in service.All())
    Console.WriteLine($"  {p}");

Console.WriteLine($"Пошук (залишок 0): {service.Search(p => p.Quantity == 0).Count} шт.");
Console.WriteLine($"Пошук (ціна > 100): {service.Search(p => p.Price > 100m).Count} шт.");

// ───────────── Сценарій 2: відмови ─────────────
Console.WriteLine();
Console.WriteLine("=== Сценарій 2: відмови ===");
ConsoleScenario.TryDo("прихід неіснуючого id", () => service.Receive("NO-SUCH-ID", 5));
ConsoleScenario.TryDo("видача більша за залишок", () => service.Issue(created.Id, 1000));
ConsoleScenario.TryDo("нульова кількість приходу", () => service.Receive(created.Id, 0));
ConsoleScenario.TryDo("порожня назва нового товару", () => service.Add(" ", 1m, 1));
ConsoleScenario.TryDo("дубль id у сховищі", () => store.Add(Product.Create(created.Id, "Копія", 1m)));
ConsoleScenario.TryDo("видалення товару із залишком", () => service.Remove(created.Id));
ConsoleScenario.TryDo("сервіс без сховища", () => new CatalogService(null!));
Console.WriteLine($"Стан після усіх відмов: {service.Find(created.Id)}");

if (store is CachingCatalogStore cache)
    Console.WriteLine($"Кеш: влучань {cache.Hits}, промахів {cache.Misses}");

return 0;
