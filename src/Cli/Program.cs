using Core.Dto;
using Core.Import;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

// Викликаємо імпортер (назва залишилась старою)
MultiImportResult result = ProductCsvImporter.Load(path);

Console.WriteLine($"Завантажено товарів: {result.Products.Count}");
foreach (ProductDto p in result.Products)
{
    Console.WriteLine($" [Товар] {p.Id,-10} {p.Name,-20} {p.Price,8:F2} {p.Note}");
}

Console.WriteLine($"\nЗавантажено складів: {result.Warehouses.Count}");
foreach (WarehouseDto w in result.Warehouses)
{
    Console.WriteLine($" [Склад] {w.Id,-10} {w.Name,-20} {w.Location}");
}

Console.WriteLine(new string('-', 60));

if (result.Errors.Count > 0)
{
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
    {
        Console.WriteLine($" ! {e}");
    }
    Console.WriteLine(new string('-', 60));
}

// Статистика імпорту[cite: 1]
int total = result.Products.Count + result.Warehouses.Count + result.Errors.Count;
int accepted = result.Products.Count + result.Warehouses.Count;
double errorRate = total > 0 ? (double)result.Errors.Count / total * 100 : 0;

Console.WriteLine($"Статистика: усього {total} | прийнято {accepted} | помилок {errorRate:F1}%");

return 0;