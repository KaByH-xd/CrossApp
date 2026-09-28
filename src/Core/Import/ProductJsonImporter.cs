using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    public static ImportResult<ProductDto> Load(string path)
    {
        string json = File.ReadAllText(path);
        
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        
        // Десеріалізуємо масив, якщо null — беремо порожній список []
        var rawItems = JsonSerializer.Deserialize<List<ProductDto>>(json, options) ?? [];

        var items = new List<ProductDto>();
        var errors = new List<string>();

        for (int i = 0; i < rawItems.Count; i++)
        {
            var item = rawItems[i];
            
            // Базова валідація, аналогічна CSV
            if (string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Name))
            {
                errors.Add($"об'єкт {i + 1}: ID або назва порожні");
            }
            else if (item.Price < 0)
            {
                errors.Add($"об'єкт {i + 1}: ціна не може бути від'ємною");
            }
            else
            {
                items.Add(item);
            }
        }

        return new ImportResult<ProductDto>(items, errors);
    }
}