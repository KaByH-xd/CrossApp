using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    // РОБИМО КЛАС ПУБЛІЧНИМ
    public class JsonRoot
    {
        public List<ProductDto>? Products { get; set; }
        public List<WarehouseDto>? Warehouses { get; set; }
    }

    // onSkipped (додаткове завдання 2): звіт про пропущені записи — у руках викликача.
    public static MultiImportResult Load(string path, Action<string>? onSkipped = null)
    {
        string json = File.ReadAllText(path);
        
        var options = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        };
        
        var root = JsonSerializer.Deserialize<JsonRoot>(json, options) ?? new JsonRoot();

        var products = new List<ProductDto>();
        var warehouses = new List<WarehouseDto>();
        var errors = new List<string>();

        void Skip(string reason)
        {
            errors.Add(reason);
            onSkipped?.Invoke(reason);
        }

        if (root.Products != null)
        {
            for (int i = 0; i < root.Products.Count; i++)
            {
                var p = root.Products[i];
                if (string.IsNullOrWhiteSpace(p.Id) || string.IsNullOrWhiteSpace(p.Name))
                    Skip($"Товар [індекс {i}]: ID або назва порожні");
                else if (p.Price < 0)
                    Skip($"Товар {p.Id}: ціна не може бути від'ємною");
                else
                    products.Add(p);
            }
        }

        if (root.Warehouses != null)
        {
            for (int i = 0; i < root.Warehouses.Count; i++)
            {
                var w = root.Warehouses[i];
                if (string.IsNullOrWhiteSpace(w.Id) || string.IsNullOrWhiteSpace(w.Name))
                    Skip($"Склад [індекс {i}]: ID або назва порожні");
                else
                    warehouses.Add(w);
            }
        }

        return new MultiImportResult(products, warehouses, errors);
    }
}