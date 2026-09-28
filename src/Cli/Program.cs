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

ImportResult<ProductDto> result = ProductCsvImporter.Load(path);

Console.WriteLine($"Завантажено записів: {result.Items.Count}");
Console.WriteLine(new string('-', 60));

foreach (ProductDto p in result.Items.Take(5))
{
    Console.WriteLine($" {p.Id,-10} {p.Name,-25} {p.Price,8:F2} {p.Note}");
}

Console.WriteLine(new string('-', 60));

if (result.Errors.Count > 0)
{
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
    {
        Console.WriteLine($" ! {e}");
    }
}

return 0;