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

    public static MultiImportResult Load(string path)
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

        if (root.Products != null)
        {
            for (int i = 0; i < root.Products.Count; i++)
            {
                var p = root.Products[i];
                if (string.IsNullOrWhiteSpace(p.Id) || string.IsNullOrWhiteSpace(p.Name))
                    errors.Add($"Товар [індекс {i}]: ID або назва порожні");
                else if (p.Price < 0)
                    errors.Add($"Товар {p.Id}: ціна не може бути від'ємною");
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
                    errors.Add($"Склад [індекс {i}]: ID або назва порожні");
                else
                    warehouses.Add(w);
            }
        }

        return new MultiImportResult(products, warehouses, errors);
    }
}