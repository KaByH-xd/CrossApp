using Core.Domain;
using Core.Dto;
using Core.Import;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

// ───────────── Сценарій 1: успіх ─────────────
Console.WriteLine("=== Сценарій 1: успіх ===");
Product product = Product.Create("p-001", "Цемент М400 25кг", 185.50m, 100);
Console.WriteLine(product);
product.RegisterArrival(50);
product.Issue(30);
product.ChangePrice(199.99m);
Console.WriteLine(product);

Warehouse warehouse = Warehouse.Create("wh-a", "Головний склад", "Київ");
warehouse.Relocate("Київ, вул. Складська, 1");
Console.WriteLine(warehouse);

// ───────────── Сценарій 2: порушення інваріантів ─────────────
Console.WriteLine();
Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");
TryDo("видача більша за залишок", () => product.Issue(1000));
TryDo("нульова кількість видачі", () => product.Issue(0));
TryDo("нульовий прихід", () => product.RegisterArrival(0));
TryDo("порожній Id товару", () => Product.Create(" ", "Пісок", 10m, 10));
TryDo("порожня назва товару", () => Product.Create("P-002", "", 10m, 10));
TryDo("від'ємний залишок", () => Product.Create("P-003", "Цегла", 12m, -5));
TryDo("від'ємна ціна", () => Product.Create("P-004", "Щебінь", -1m, 5));
TryDo("зміна ціни на від'ємну", () => product.ChangePrice(-10m));
TryDo("порожнє розташування складу", () => Warehouse.Create("WH-X", "Склад", " "));
TryDo("повторне розташування складу", () => warehouse.Relocate("київ, вул. складська, 1"));
TryDo("FromDto з від'ємним залишком", () => Product.FromDto(new ProductDto("P-009", "Дріт", 10m, null, -3)));
TryDo("FromDto з невідомим статусом", () => Product.FromDto(new ProductDto("P-010", "Дріт", 10m, null, 1, "Archived")));
Console.WriteLine($"Стан після усіх відмов: {product}");

// Не компілюється: у властивості немає публічного set (помилка CS0272).
// product.Quantity = -5;
// Не компілюється: конструктор приватний (помилка CS0122).
// var p = new Product(...);

// ───────────── Сценарій 3: статуси та переходи ─────────────
Console.WriteLine();
Console.WriteLine("=== Сценарій 3: статуси та переходи ===");
product.ChangeStatus(ProductStatus.Suspended);
Console.WriteLine(product);
TryDo("видача призупиненого товару", () => product.Issue(1));
product.ChangeStatus(ProductStatus.Active);
product.ChangeStatus(ProductStatus.Discontinued);
Console.WriteLine(product);
TryDo("повернення зі знятого з обігу", () => product.ChangeStatus(ProductStatus.Active));
TryDo("прихід знятого з обігу", () => product.RegisterArrival(10));

// ───────────── Сценарій 4: правило на дві сутності ─────────────
Console.WriteLine();
Console.WriteLine("=== Сценарій 4: правило на дві сутності (сервіс) ===");
var placement = new WarehousePlacementService(maxProductsPerWarehouse: 2);
Warehouse reserve = Warehouse.Create("WH-B", "Резервний склад", "Львів");
Product sand = Product.Create("P-020", "Пісок", 40m, 10);
Product brick = Product.Create("P-021", "Цегла", 12m, 500);
Product gravel = Product.Create("P-022", "Щебінь", 30m, 70);
placement.Place(warehouse, sand);
placement.Place(warehouse, brick);
Console.WriteLine($"{warehouse.Id}: {string.Join(", ", placement.GetProductIds(warehouse))}");
TryDo("склад заповнений", () => placement.Place(warehouse, gravel));
TryDo("товар уже на іншому складі", () => placement.Place(reserve, sand));
placement.Place(reserve, gravel);
Console.WriteLine($"{reserve.Id}: {string.Join(", ", placement.GetProductIds(reserve))}");

// ───────────── Сценарій 5: імпорт → сутності ─────────────
Console.WriteLine();
Console.WriteLine("=== Сценарій 5: імпорт → сутності ===");
string path = args.Length > 0 ? args[0] : Path.Combine("data", "lab04.json");
if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

string extension = Path.GetExtension(path).ToLowerInvariant();
MultiImportResult imported = extension switch
{
    ".csv" => ProductCsvImporter.Load(path),
    ".json" => ProductJsonImporter.Load(path),
    _ => throw new NotSupportedException($"Формат файлу '{extension}' не підтримується")
};

Console.WriteLine($"Імпорт (формат): товарів {imported.Products.Count}, складів {imported.Warehouses.Count}, відхилено {imported.Errors.Count}");
foreach (string e in imported.Errors)
    Console.WriteLine($"  ! {e}");

DomainImportResult domain = DomainConverter.ToDomain(imported);
Console.WriteLine($"Домен (правила): товарів {domain.Products.Count}, складів {domain.Warehouses.Count}, відхилено {domain.Errors.Count}");
foreach (Product p in domain.Products)
    Console.WriteLine($"  [Товар] {p}");
foreach (Warehouse w in domain.Warehouses)
    Console.WriteLine($"  [Склад] {w}");
foreach (string e in domain.Errors)
    Console.WriteLine($"  ! {e}");

return 0;

// Один обробник винятків для всіх сценаріїв: на екран іде лише Message, без stack trace.
static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}
