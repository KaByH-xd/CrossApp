using System.Globalization;
using Core.Dto;

namespace Core.Import;

// Назву класу не змінюємо, щоб не було помилки CS0103
public static class ProductCsvImporter
{
    private const char Separator = ';';

    public static MultiImportResult Load(string path)
    {
        var products = new List<ProductDto>();
        var warehouses = new List<WarehouseDto>();
        var errors = new List<string>();
        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
            
            // Пропускаємо заголовок (тепер починається зі слова type)
            if (number == 1 && line.StartsWith("type", StringComparison.OrdinalIgnoreCase)) continue;

            switch (ParseLine(line))
            {
                case ParseProductOk p:
                    products.Add(p.Value);
                    break;
                case ParseWarehouseOk w:
                    warehouses.Add(w.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }
        return new MultiImportResult(products, warehouses, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            // Патерни для Товарів (префікс "P")[cite: 1]
            ["P", "", ..] or ["P", _, "", ..] 
                => new ParseFailed("ID або назва товару порожні"),
            ["P", var id, var name, var priceStr] when decimal.TryParse(priceStr, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal p) && p >= 0 
                => new ParseProductOk(new ProductDto(id, name, p)),
            ["P", var id, var name, var priceStr, var note] when decimal.TryParse(priceStr, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal p) && p >= 0 
                => new ParseProductOk(new ProductDto(id, name, p, string.IsNullOrWhiteSpace(note) ? null : note)),
            ["P", ..] 
                => new ParseFailed("Некоректний формат або ціна для товару"),

            // Патерни для Складів (префікс "W")[cite: 1]
            ["W", "", ..] or ["W", _, "", ..] 
                => new ParseFailed("ID або назва складу порожні"),
            ["W", var id, var name, var location] 
                => new ParseWarehouseOk(new WarehouseDto(id, name, location)),
            ["W", ..] 
                => new ParseFailed("Некоректний формат складу"),

            // Гілка для невідомих префіксів
            _ => new ParseFailed($"Невідомий префікс рядка або замало даних: {parts[0]}")
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseProductOk(ProductDto Value) : ParseOutcome;
    private sealed record ParseWarehouseOk(WarehouseDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}