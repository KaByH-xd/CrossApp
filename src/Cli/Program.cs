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

// Вибір імпортера за розширенням файлу через switch expression
string extension = Path.GetExtension(path).ToLowerInvariant();
ImportResult<ProductDto> result = extension switch
{
    ".csv" => ProductCsvImporter.Load(path),
    ".json" => ProductJsonImporter.Load(path),
    _ => throw new NotSupportedException($"Формат файлу '{extension}' не підтримується")
};

Console.WriteLine($"Завантажено записів: {result.Items.Count}");
Console.WriteLine(new string('-', 60));

foreach (ProductDto p in result.Items.Take(5))
{
    Console.WriteLine($" {p.Id,-10} {p.Name,-25} {p.Price,8:F2} {p.Note}");
}

Console.WriteLine(new string('-', 60));

if (result.Errors.Count > 0)
{
    Console.WriteLine($"Пропущено рядків/об'єктів: {result.Errors.Count}");
    foreach (string e in result.Errors)
    {
        Console.WriteLine($" ! {e}");
    }
    Console.WriteLine(new string('-', 60));
}

// ДОДАТКОВЕ ЗАВДАННЯ 3: Статистика імпорту
int total = result.Items.Count + result.Errors.Count;
int accepted = result.Items.Count;
int skipped = result.Errors.Count;
double errorRate = total > 0 ? (double)skipped / total * 100 : 0;

Console.WriteLine($"Статистика: усього {total} | прийнято {accepted} | пропущено {skipped} | помилок {errorRate:F1}%");

return 0;